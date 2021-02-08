using UnityEngine;


namespace Utils
{
    public class ScreenInDegree : MonoBehaviour
    {
        private ScreenOrientation _currentScreenOrientation;
        

        public int Rotation => RotationZ();
        public int Width => SetWidth();
        
        private int RotationZ()
        {
            int rotationZ;
            _currentScreenOrientation = Screen.orientation;
            switch (_currentScreenOrientation)
            {
                case ScreenOrientation.Portrait:
                    rotationZ = 90;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    rotationZ = 180;
                    break;
                default:
                    rotationZ = 0;
                    break;
            }

            return rotationZ;
        }

        private int SetWidth()
        {
            if (Rotation == 180 || Rotation == 0)
                return 2280;
            return 1080;
        }
    }
}