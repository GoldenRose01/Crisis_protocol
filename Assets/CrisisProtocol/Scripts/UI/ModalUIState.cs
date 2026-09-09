using System;
using UnityEngine;

namespace GoldenCast.UI
{
    public static class ModalUIState
    {
        private static string activeOwner;
        private static float previousTimeScale = 1f;
        private static CursorLockMode previousLockState = CursorLockMode.None;
        private static bool previousCursorVisible = true;

        public static bool IsModalOpen => !string.IsNullOrEmpty(activeOwner);
        public static string ActiveOwner => activeOwner;

        public static event Action<string> ModalOpened;
        public static event Action<string> ModalClosed;

        public static bool TryOpen(string owner, bool pauseGameplay = true, bool unlockCursor = true)
        {
            if (string.IsNullOrWhiteSpace(owner))
                return false;

            if (IsModalOpen && activeOwner != owner)
                return false;

            if (activeOwner == owner)
                return true;

            activeOwner = owner;
            previousTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            if (pauseGameplay)
                Time.timeScale = 0f;

            if (unlockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            ModalOpened?.Invoke(owner);
            return true;
        }

        public static bool IsOwner(string owner)
        {
            return activeOwner == owner;
        }

        public static void Close(string owner)
        {
            if (!IsOwner(owner))
                return;

            string closedOwner = activeOwner;
            activeOwner = null;
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
            ModalClosed?.Invoke(closedOwner);
        }

        public static void ForceCloseAll()
        {
            if (!IsModalOpen)
                return;

            string closedOwner = activeOwner;
            activeOwner = null;
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
            ModalClosed?.Invoke(closedOwner);
        }
    }
}
