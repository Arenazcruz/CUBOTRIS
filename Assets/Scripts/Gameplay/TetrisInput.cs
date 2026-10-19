using UnityEngine;
using UnityEngine.InputSystem;

namespace Cubotris.Gameplay
{
    [DefaultExecutionOrder(-100)]
    public sealed class TetrisInput : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField, Min(0.01f)] private float repeatDelay = 0.16f;
        [SerializeField, Min(0.01f)] private float repeatInterval = 0.075f;
        private int heldDirection;
        private float repeatTime;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !controller.AcceptsInput)
            {
                ResetInput();
                return;
            }
            controller.SoftDropHeld = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                controller.HardDrop();
                return;
            }
            int direction = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            if (direction != heldDirection)
            {
                heldDirection = direction;
                repeatTime = Mathf.Max(0.01f, repeatDelay);
                if (direction != 0) controller.Move(direction);
            }
            else if (direction != 0)
            {
                repeatTime -= Time.deltaTime;
                while (repeatTime <= 0)
                {
                    controller.Move(direction);
                    repeatTime += Mathf.Max(0.01f, repeatInterval);
                }
            }
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                controller.Rotate();
        }

        private void OnDisable() { ResetInput(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }

        private void ResetInput()
        {
            heldDirection = 0;
            repeatTime = 0;
            if (controller != null) controller.SoftDropHeld = false;
        }
    }
}
