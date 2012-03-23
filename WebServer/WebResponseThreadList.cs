using System;
using Microsoft.SPOT;
using System.Threading;
using System.Collections;

namespace WebServer
{
    public class WebResponseThreadList
    {
        public const int MAX_THREADS = 4;
        private readonly WebResponseThread[] _RequestWorkers = new WebResponseThread[MAX_THREADS];
        private readonly bool[] _StillRunning = new bool[MAX_THREADS];
        private bool _Stopping = false;

        public WebResponseThreadList()
        {
            lock (_RequestWorkers)
            {
                for (int i = 0; i < MAX_THREADS; i++)
                {
                    _RequestWorkers[i] = null;
                    _StillRunning[i] = false;
                }
            }
        }

        public int Enqueue(WebResponseThread item)
        {
            int result = -1;
            if (!_Stopping)
            {
                lock (_RequestWorkers) 
                for (int i = 0; i < MAX_THREADS; i++)
                { // remove stopped threads from list
                    if (_RequestWorkers[i] != null && _StillRunning[i] == false)
                    {
                        _RequestWorkers[i] = null;
                    }
                }
                

                { // find spare thread
                    for (int i = 0; i < MAX_THREADS; i++)
                    {
                        if (_RequestWorkers[i] == null)
                        {
                            _RequestWorkers[i] = item;
                            _RequestWorkers[i].WorkerID = i;
                            _StillRunning[i] = true;
                            result = i;
                            break;
                        }
                    }
                }
            }
            Debug.Print("add workerid: " + result.ToString());
            return result;
        }

        public void RemoveAt(int position)
        {
            Debug.Print("RemoveAt workerid: " + position.ToString());
            _StillRunning[position] = false;
        }

        public void StopAll()
        {
            _Stopping = true;
            for (int i = 0; i < WebResponseThreadList.MAX_THREADS; i++)
            {
                if (_RequestWorkers[i] != null)
                {
                    _RequestWorkers[i].Stop();
                }
                _StillRunning[i] = false;
                Thread.Sleep(50);
            }            
        }
    }
}
