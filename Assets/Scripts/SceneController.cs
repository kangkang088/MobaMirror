using Mirror.Examples.NetworkRoom;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MirrorExample
{
    public class SceneController : MonoBehaviour
    {
        public NetworkRoomManagerExt roomManagerExt;

        public void ChooseScene()
        {
            IEnumerable<Toggle> toggles = GetComponent<ToggleGroup>().ActiveToggles();
            foreach(Toggle toggle in toggles)
            {
                if(toggle.isOn)
                {
                    roomManagerExt.GameplayScene = toggle.name;
                }
            }
        }

    }

}