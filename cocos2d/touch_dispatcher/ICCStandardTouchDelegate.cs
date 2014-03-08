
using System.Collections.Generic;

namespace CocosSharp
{

    public interface ICCStandardTouchDelegate : ICCTouchDelegate
    {
        // optional
        void TouchesBegan(List<CCTouch> touches);
        void TouchesMoved(List<CCTouch> touches);
        void TouchesEnded(List<CCTouch> touches);
        void TouchesCancelled(List<CCTouch> touches);
    }
}