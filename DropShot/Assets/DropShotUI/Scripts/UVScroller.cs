using UnityEngine;
using UnityEngine.UI;

namespace DropShot.UI
{
    // 让棋盘格地面 / 波点底纹慢慢滚动，背景不至于是死图
    [RequireComponent(typeof(RawImage))]
    public class UVScroller : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0.03f, 0f);
        RawImage img;

        void Awake() => img = GetComponent<RawImage>();

        void Update()
        {
            Rect r = img.uvRect;
            r.position += speed * Time.unscaledDeltaTime;
            r.x %= 1f;
            r.y %= 1f;
            img.uvRect = r;
        }
    }
}
