using UnityEngine;

namespace WoodLand3D.CameraSystems
{
    /// <summary>
    /// Simple smooth camera follow / focus helper.
    /// If no target is assigned, only FocusOnTile is used.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.25f;

        private Vector3 _velocity;
        private Vector3? _focusPoint;

        private void LateUpdate()
        {
            Vector3 desired = transform.position;

            if (_focusPoint.HasValue)
            {
                var p = _focusPoint.Value;
                desired = new Vector3(p.x, transform.position.y, p.z);
            }
            else if (target != null)
            {
                desired = new Vector3(target.position.x, transform.position.y, target.position.z);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        /// <summary>
        /// Ask the camera to gently move its position toward a tile.
        /// </summary>
        public void FocusOnTile(Vector3 tilePos)
        {
            _focusPoint = tilePos;
        }

        /// <summary>
        /// Clear any explicit focus so the camera goes back to tracking its target.
        /// </summary>
        public void ClearFocus()
        {
            _focusPoint = null;
        }
    }
}

