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
        private int inputVersion;
        private bool waitForRelease;
        private int bindingVersion = -1;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (inputVersion != controller.InputVersion || bindingVersion != ControlBindings.Version)
            {
                inputVersion = controller.InputVersion;
                bindingVersion = ControlBindings.Version;
                ResetInput();
                waitForRelease = true;
            }
            if (keyboard == null || !controller.AcceptsInput || ControlBindings.Capturing)
            {
                ResetInput();
                return;
            }
            // Keys used in the pause UI must be released before controlling the piece again.
            if (waitForRelease)
            {
                waitForRelease = ControlBindings.AnyGameplayHeld();
                ResetInput();
                return;
            }
            controller.SoftDropHeld = ControlBindings.Held(GameAction.SoftDrop);
            if (ControlBindings.Pressed(GameAction.Hold))
            {
                controller.Hold();
                return;
            }
            if (ControlBindings.Pressed(GameAction.HardDrop))
            {
                controller.HardDrop();
                return;
            }
            int direction = (ControlBindings.Held(GameAction.Right) ? 1 : 0)
                - (ControlBindings.Held(GameAction.Left) ? 1 : 0);
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
            if (ControlBindings.Pressed(GameAction.Rotate))
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
