using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MySettings = SABotSupport.Properties.Settings;
using AssaInstance = SABotSupport.AssaAutomation.AssaInstance;
using System.Text;
using System.Diagnostics;

namespace SABotSupport
{
    internal static class GameStatePoller
    {
        public static BindingList<AssaInstance> AttachedAssaInstances { get; } = new();

        internal delegate void NewDataAvailableEventHandler(object sender, NewDataAvailableEventArgs e);
        internal static event NewDataAvailableEventHandler NewDataAvailableEvent = delegate { };  //avoids null check when firing events
        internal delegate void NewStatusMessageAvailableEventHandler(object sender, NewStatusMessageAvailableEventArgs e);
        internal static event NewStatusMessageAvailableEventHandler NewStatusMessageAvailableEvent = delegate { };
        internal delegate void AttachedInstanceListChangedEventHandler(object sender, AttachedInstanceListChangedEventArgs e);
        internal static event AttachedInstanceListChangedEventHandler AttachedInstanceListChangedEvent = delegate { };

        private static readonly ConcurrentDictionary<AssaInstance, DataPackage> cachedData = new();
        private static CancellationTokenSource cancellationTokenSource = new();
        private static volatile bool isRunning = false;

        public static void StopPolling()
        {
            cancellationTokenSource.Cancel();
            isRunning = false;
            AttachedAssaInstances.Clear();  //UI thread
            cachedData.Clear();
        }

        public static async Task StartPolling(AssaInstance newInstance, DataPackage data = default)
        {
            if (data.IsInitialized)
            {
                cachedData[newInstance] = data;
            }

            await StartPolling(Enumerable.Repeat(newInstance, 1)).ConfigureAwait(false);
        }
        public static async Task StartPolling(IEnumerable<AssaInstance> newInstances)
        {
            foreach (var instance in newInstances)
            {
                AttachedAssaInstances.Add(instance);    //UI thread
            }

            Trace.WriteLine($"[GameStatePoller] added {newInstances.Count()} new instances");

            if (isRunning)
                return;

            cancellationTokenSource.Dispose();
            cancellationTokenSource = new();
            isRunning = true;

            try
            {
                await Task.Run(() => PollFunction(cancellationTokenSource.Token), cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        public static DataPackage GetData(AssaInstance instance)
            => (instance != default && cachedData.TryGetValue(instance, out DataPackage value)) ? value : new();
                
        private static async Task PollFunction(CancellationToken cancelToken)
        {
            while (true)
            {
                StringBuilder sb = new();

                //iterate on a copy of the list
                //this allows us to make changes to the list ourselves
                //this also allows other threads to add to the list which we will process on the next polling
                foreach (var instance in AttachedAssaInstances.ToArray())
                {
                    cancelToken.ThrowIfCancellationRequested();

                    //remove dead instances
                    if (!instance.IsAlive())
                    {
                        AttachedAssaInstances.Remove(instance);
                        cachedData.TryRemove(instance, out _);
                        AttachedInstanceListChangedEvent(null, new(instance));

                        Trace.WriteLine($"[GameStatePoller] removing process ID {instance.ProcessID}, reason: instance.IsAlive is false");

                        continue;
                    }

                    var data = MemoryReader.GetAssaData(instance.ProcessID, skipGameClientData: false);

                    cancelToken.ThrowIfCancellationRequested();

                    //remove instances where GetAssaDate failed
                    if (!data.IsInitialized)
                    {
                        AttachedAssaInstances.Remove(instance);
                        cachedData.TryRemove(instance, out _);
                        AttachedInstanceListChangedEvent(null, new(instance));

                        Trace.WriteLine($"[GameStatePoller] removing process ID {instance.ProcessID}, reason: data.IsInitialized is false");

                        continue;
                    }

                    cachedData[instance] = data;

                    //update cached info if different
                    if (instance.CachedAccount != data.GameClientData.CurrentAccount ||
                        instance.CachedCharacter != data.GameClientData.CurrentCharacter ||
                        instance.CachedName != data.Player.Name)
                    {
                        instance.CachedAccount = data.GameClientData.CurrentAccount;
                        instance.CachedCharacter = data.GameClientData.CurrentCharacter;
                        instance.CachedName = data.Player.Name;
                        AttachedInstanceListChangedEvent(null, null);

                        Trace.WriteLine($"[GameStatePoller] updating process ID {instance.ProcessID}, new name is {data.Player.Name}");
                    }

                    //update status message if necessary
                    if (!data.IsOnline && !String.IsNullOrEmpty(data.Player.Name))
                    {
                        sb.Append($" {data.Player.Name}[斷線]");
                    }
                    if (data.Items.Any(x => x.Name == "精靈勇者信物" && x.IsPresent))
                    {
                        sb.Append($" {data.Player.Name}[精召]");
                    }

                    NewDataAvailableEvent(null, new(instance, data));
                }

                NewStatusMessageAvailableEvent(null, new(sb.ToString()));

                await Task.Delay(Convert.ToInt32(MySettings.Default.UpdateDelay), cancelToken).ConfigureAwait(false);
            }
        }
    }

    internal class NewDataAvailableEventArgs : EventArgs
    {
        public AssaInstance Instance { get; }
        public DataPackage Data { get; }

        public NewDataAvailableEventArgs(AssaInstance instance, DataPackage data)
        {
            Instance = instance;
            Data = data;
        }
    }

    internal class NewStatusMessageAvailableEventArgs : EventArgs
    {
        public string Message { get; }

        public NewStatusMessageAvailableEventArgs(string message)
        {
            Message = message;
        }
    }

    internal class AttachedInstanceListChangedEventArgs : EventArgs
    {
        public AssaInstance DeadInstance { get; }

        public AttachedInstanceListChangedEventArgs(AssaInstance deadInstance)
        {
            DeadInstance = deadInstance;
        }
    }
}
