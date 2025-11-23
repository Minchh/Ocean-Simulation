// Inspired by Adam Viola: https://github.com/adamviola/simple-free-look-camera

using Godot;
using System;

public partial class CameraController : Node3D
{
    private const float SHIFT_MULTIPLIER = 1.5f;
    private const float ALT_MULTIPLIER = 1.0f / SHIFT_MULTIPLIER;
    
    [Export(PropertyHint.Range, "0.0,1.0")]
    private float m_Sensitivity = 0.25f;
    
    // Mouse states
    private Vector2 m_MousePosition = Vector2.Zero;
    private float m_TotalPitch = 0.0f;
    
    // Movement state
    private Vector3 m_Direction = Vector3.Zero;
    private Vector3 m_Velocity = Vector3.Zero;
    private float m_Acceleration = 30.0f;
    private float m_Deceleration = -10.0f;
    private float m_VelocityMultiplier = 4.0f;
    
    // Movement modifer keyboard state
    private bool m_Shift = false;
    private bool m_Alt = false;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            m_MousePosition = mouseMotionEvent.ScreenRelative;
        }

        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            switch (mouseButtonEvent.ButtonIndex)
            {
                case MouseButton.Right:
                    Input.SetMouseMode(mouseButtonEvent.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible);
                    break;
                case MouseButton.WheelUp:
                    m_VelocityMultiplier = Mathf.Clamp(m_VelocityMultiplier * 1.1f, 0.2f, 20.0f);
                    break;
                case MouseButton.WheelDown:
                    m_VelocityMultiplier = Mathf.Clamp(m_VelocityMultiplier / 1.1f, 0.2f, 20.0f);
                    break;
            }
        }

        if (@event is InputEventKey keyEvent)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Shift:
                    m_Shift = keyEvent.Pressed;
                    break;
                case Key.Alt:
                    m_Alt = keyEvent.Pressed;
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        UpdateMouseLook();
        UpdateMovement((float)delta);
    }

    private void UpdateMovement(float delta)
    {
        m_Direction = new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down"),
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward")
        );
        // Compute the change in velocity due to desired direction and "drag"
        // The "drag" is a constant acceleration on the camera to bring its velocity to 0
        Vector3 offset = m_Direction.Normalized() * m_Acceleration * m_VelocityMultiplier * delta
                     + m_Velocity.Normalized() * m_Deceleration * m_VelocityMultiplier * delta;

        float speedMultiplier = 1.0f;
        if (m_Shift) speedMultiplier *= SHIFT_MULTIPLIER;
        if (m_Alt) speedMultiplier *= ALT_MULTIPLIER;

        if (m_Direction == Vector3.Zero && offset.LengthSquared() > m_Velocity.LengthSquared())
        {
            m_Velocity = Vector3.Zero;
        }
        else
        {
            m_Velocity.X = Mathf.Clamp(m_Velocity.X + offset.X, -m_VelocityMultiplier, m_VelocityMultiplier);
            m_Velocity.Y = Mathf.Clamp(m_Velocity.Y + offset.Y, -m_VelocityMultiplier, m_VelocityMultiplier);
            m_Velocity.Z = Mathf.Clamp(m_Velocity.Z + offset.Z, -m_VelocityMultiplier, m_VelocityMultiplier);
            
            Translate(m_Velocity * delta * speedMultiplier);
        }
    }

    private void UpdateMouseLook()
    {
        if (Input.GetMouseMode() == Input.MouseModeEnum.Captured)
        {
            m_MousePosition *= m_Sensitivity;
            float yaw = m_MousePosition.X;
            float pitch = m_MousePosition.Y;
            m_MousePosition = Vector2.Zero;
            
            // Prevent looking up/down too far
            pitch = Mathf.Clamp(pitch, -89.0f - m_TotalPitch, 89.0f - m_TotalPitch);
            m_TotalPitch += pitch;
            
            RotateY(Mathf.DegToRad(-yaw));
            RotateObjectLocal(Vector3.Right, Mathf.DegToRad(-pitch));
        }
    }
}
