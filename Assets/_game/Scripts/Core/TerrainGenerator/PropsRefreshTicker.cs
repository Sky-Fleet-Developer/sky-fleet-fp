using System.Threading.Tasks;
using Cinemachine;
using Core.Game;
using UnityEngine;

namespace Core.TerrainGenerator
{
    public class PropsRefreshTicker
    {
        private Vector3 lastPosition;
        private float distanceToUpdateSqr;
        private CinemachineBrain target;
        private const int TickDelay = 500;
        private TerrainProvider terrain;
        private Task loop;
        private TaskCancellationToken loopToken;
        
        public PropsRefreshTicker(CinemachineBrain target, float distanceToUpdate, TerrainProvider terrain)
        {
            this.target = target;
            this.terrain = terrain;
            lastPosition = TargetPosition;
            distanceToUpdateSqr = distanceToUpdate * distanceToUpdate;
        }

        public void TryRun()
        {
            if (loop != null)
            {
                loopToken.Cancel();
                Debug.LogWarning("Loop is already run!");
            }

            if (!IsRunValid) return;
            
            loopToken = new TaskCancellationToken();
            loop = TickLoop(loopToken);
        }

        private bool IsRefreshValid(Vector3 currentPosition)
        {
            if (Vector3.SqrMagnitude(currentPosition - lastPosition) < distanceToUpdateSqr) return false;
            
            return true;
        }

        private async Task TickLoop(TaskCancellationToken token)
        {
            while (IsRunValid)
            {
                if (token.IsCancelled) return;
                
                Tick();
                await Task.Delay(TickDelay);
            }
            Stop();
        }

        private static bool IsRunValid => Application.isPlaying;

        public void Stop()
        {
            loopToken.Cancel();
            loop = null;
        }
        
        private void Tick()
        {
            if (!IsRefreshValid(TargetPosition)) return;
            
            lastPosition = TargetPosition;
            terrain.RefreshProps();
        }

        private Vector3 TargetPosition
        {
            get
            {
                Vector3 pos;
                ICinemachineCamera activeVirtualCamera = target.ActiveVirtualCamera;
                if (activeVirtualCamera == null)
                {
                    pos = target.transform.position;
                }
                else
                {
                    pos  = activeVirtualCamera.VirtualCameraGameObject.transform.position - WorldOffset.Offset;
                }
                pos.y = 0;
                return pos;
            }
        }

        private class TaskCancellationToken
        {
            public bool IsCancelled { get; private set; }

            public void Cancel()
            {
                IsCancelled = true;
            }
        }
    }
}
