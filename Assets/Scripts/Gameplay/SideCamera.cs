using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Камера строго сбоку: следует за целью по X и Y, Z фиксирован.
    /// Смотрит вперёд по ходу движения, чтобы было видно, куда бежишь.
    /// </summary>
    public class SideCamera : MonoBehaviour
    {
        public Transform target;

        [Header("Положение")]
        public float distance = 7f;          // насколько камера отодвинута от плоскости игры
        public float height = 1.5f;          // над Коррой
        public float lookAhead = 1.5f;       // сдвиг вперёд по направлению взгляда

        [Header("Плавность, сек")]
        public float smoothTime = 0.25f;

        Vector3 _velocity;
        float _ahead;

        void LateUpdate()
        {
            if (target == null) return;

            // Корра повёрнута на +90° (вправо) или -90° (влево) — берём знак forward.x.
            float dir = Mathf.Sign(target.forward.x);
            _ahead = Mathf.Lerp(_ahead, dir * lookAhead, Time.deltaTime * 2f);

            var wanted = new Vector3(target.position.x + _ahead, target.position.y + height, -distance);
            transform.position = Vector3.SmoothDamp(transform.position, wanted, ref _velocity, smoothTime);
            transform.rotation = Quaternion.Euler(8f, 0f, 0f); // лёгкий взгляд сверху
        }
    }
}
