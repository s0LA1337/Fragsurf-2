using Fragsurf.Shared;
using UnityEngine;

namespace Fragsurf.UI
{
    public class MainMenu : MonoBehaviour
    {

        public void OpenModal(string name)
        {
            UGuiManager.Instance.OpenModal(name);
        }

        public void OpenUrl(string url)
        {
            var dialog = UGuiManager.Instance.Find<Modal_Dialog>();
            if (!dialog)
            {
                Application.OpenURL(url);
                return;
            }
            dialog.Confirmation("Go to website?", "This will open your browser to the following address: " + url, () =>
            {
                Application.OpenURL(url);
            });
        }

        public void QuitGame()
        {
            var dialog = UGuiManager.Instance.Find<Modal_Dialog>();
            if (!dialog)
            {
                FSGameLoop.Quit();
                return;
            }
            dialog.Confirmation("Exit Fragsurf", "Are you sure you want to quit the game?", () =>
            {
                FSGameLoop.Quit();
            });
        }

    }
}