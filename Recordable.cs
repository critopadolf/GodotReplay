using Godot;
using System;
using System.Collections.Generic;
using Dict = Godot.Collections.Dictionary;

namespace Recordable
{
    /// <summary>
    /// <para>Interface used for serializing and deserializing the state of an object.</para>
    /// </summary>
    interface IRecordable
    {
        public string UniqueId();
        public abstract Dict Serialize();
        public abstract void Deserialize(Dict dict);
    }

    class GDScriptRecordable(Node p_node) : IRecordable
    {
        private readonly Node node = p_node;
        public string UniqueId()
        {
            return node.Call("UniqueId").As<String>();
        }
        public Dict Serialize()
        {
            return node.Call("Serialize").As<Dict>();
        }
        public void Deserialize(Dict dict)
        {
            node.Call("Deserialize", [dict]);
        }
    }

    class CharacterBody3DRecordable(CharacterBody3D p_cb) : IRecordable
    {
        private readonly CharacterBody3D cb = p_cb;
        public string UniqueId()
        {
            return cb.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = cb.GlobalTransform,
                ["Velocity"] = cb.Velocity,
                ["UpDirection"] = cb.UpDirection
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            cb.GlobalTransform = dict["GlobalTransform"].As<Transform3D>();
            cb.Velocity = dict["Velocity"].As<Vector3>();
            cb.UpDirection = dict["UpDirection"].As<Vector3>();
        }
    }

    class RigidBody3DRecordable(RigidBody3D p_rb) : IRecordable
    {
        private readonly RigidBody3D rb = p_rb;

        public string UniqueId()
        {
            return rb.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = rb.GlobalTransform,
                ["AngularVelocity"] = rb.AngularVelocity,
                ["LinearVelocity"] = rb.LinearVelocity
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            rb.GlobalTransform = dict["GlobalTransform"].As<Transform3D>();
            rb.AngularVelocity = dict["AngularVelocity"].As<Vector3>();
            rb.LinearVelocity = dict["LinearVelocity"].As<Vector3>();
        }

    }

    class Node3DRecordable(Node3D p_node) : IRecordable
    {
        private readonly Node3D node = p_node;
        public string UniqueId()
        {
            return node.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = node.GlobalTransform,
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            node.GlobalTransform = dict["GlobalTransform"].As<Transform3D>();
        }
    }

    class CharacterBody2DRecordable(CharacterBody2D p_cb) : IRecordable
    {
        private readonly CharacterBody2D cb = p_cb;
        public string UniqueId()
        {
            return cb.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = cb.GlobalTransform,
                ["Velocity"] = cb.Velocity,
                ["UpDirection"] = cb.UpDirection
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            cb.GlobalTransform = dict["GlobalTransform"].As<Transform2D>();
            cb.Velocity = dict["Velocity"].As<Vector2>();
            cb.UpDirection = dict["UpDirection"].As<Vector2>();
        }
    }

    class RigidBody2DRecordable(RigidBody2D p_rb) : IRecordable
    {
        private readonly RigidBody2D rb = p_rb;

        public string UniqueId()
        {
            return rb.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = rb.GlobalTransform,
                ["AngularVelocity"] = rb.AngularVelocity,
                ["LinearVelocity"] = rb.LinearVelocity
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            rb.GlobalTransform = dict["GlobalTransform"].As<Transform2D>();
            rb.AngularVelocity = dict["AngularVelocity"].As<float>();
            rb.LinearVelocity = dict["LinearVelocity"].As<Vector2>();
        }

    }

    class Node2DRecordable(Node2D p_node) : IRecordable
    {
        private readonly Node2D node = p_node;
        public string UniqueId()
        {
            return node.GetPath();
        }

        public Dict Serialize()
        {
            var dict = new Dict
            {
                ["GlobalTransform"] = node.GlobalTransform,
            };
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            node.GlobalTransform = dict["GlobalTransform"].As<Transform2D>();
        }
    }

    class EventRecordable : IRecordable
    {
        private readonly InputEvent m_event;
        enum EventTypes
        {
            InputEventMouseMotion,
            InputEventKey,
            Default
        }
        public EventRecordable(InputEvent p_event)
        {
            m_event = p_event;
        }
        public EventRecordable()
        {
        }
        public string UniqueId()
        {
            return null;
        }
        public Dict Serialize()
        {
            Dict dict = [];
            if (m_event is InputEventMouseMotion mouseMotion)
            {
                dict["type"] = (ulong)EventTypes.InputEventMouseMotion;
                dict["Relative"] = mouseMotion.Relative;
                dict["Velocity"] = mouseMotion.Velocity;
                dict["Position"] = mouseMotion.Position;
                dict["Pressure"] = mouseMotion.Pressure;
            }
            else if (m_event is InputEventKey eventKey)
            {
                dict["type"] = (ulong)EventTypes.InputEventKey;
                dict["Keycode"] = (ulong)eventKey.GetPhysicalKeycodeWithModifiers();
                dict["Pressed"] = eventKey.Pressed;
            }
            else
            {
                dict["type"] = (ulong)EventTypes.Default;
            }
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            int Device = -1;
            EventTypes event_type = (EventTypes)(dict["type"].As<ulong>());
            switch (event_type)
            {
                case EventTypes.InputEventMouseMotion:
                    {
                        InputEventMouseMotion mouseMotion = new()
                        {
                            Relative = dict["Relative"].AsVector2(),
                            Velocity = dict["Velocity"].AsVector2(),
                            Position = dict["Position"].AsVector2(),
                            Pressure = dict["Pressure"].As<float>(),
                            Device = Device
                        };
                        Input.ParseInputEvent(mouseMotion);
                        Input.WarpMouse(mouseMotion.GlobalPosition);
                        break;
                    }
                case EventTypes.InputEventKey:
                    {
                        InputEventKey eventKey = new()
                        {
                            Keycode = (Key)dict["Keycode"].As<ulong>(),
                            Pressed = dict["Pressed"].AsBool(),
                            Device = Device
                        };
                        Input.ParseInputEvent(eventKey);
                        break;
                    }
            }
        }
        public static bool filter(InputEvent @event)
        {
            if (@event is InputEventMouseMotion ||
                @event is InputEventKey           )
            {
                return @event.Device >= 0;
            }
            return false;
        }
    }

    class InputRecordable : IRecordable
    {
        private readonly List<EventRecordable> events_this_frame;
        public InputRecordable()
        {
            events_this_frame = [];
        }
        public void Event(InputEvent @event)
        {
            events_this_frame.Add(new EventRecordable(@event));
        }
        public void ClearFrame()
        {
            events_this_frame.Clear();
        }
        public string UniqueId()
        {
            return "@input";
        }
        public Dict Serialize()
        {
            var dict = new Dict();
            foreach (string action in InputMap.GetActions())
            {
                if (action.StartsWith("debug"))
                    continue;
                float action_strength = Input.GetActionStrength(action);
                if (action_strength != 0)
                {
                    dict[action] = action_strength;
                }
                else if (Input.IsActionJustReleased(action))
                {
                    dict[action] = 0;
                }
            }
            var events = new Godot.Collections.Array<Dict>();
            foreach (EventRecordable event_record in events_this_frame)
            {
                events.Add(event_record.Serialize());
            }
            dict["@events"] = events;
            dict["MouseMode"] = (ulong)Input.MouseMode;
            return dict;
        }
        public void Deserialize(Dict dict)
        {
            foreach (string action in InputMap.GetActions())
            {
                if (!dict.TryGetValue(action, out var value))
                {
                    continue;
                }
                float action_strength = value.As<float>();
                if (action_strength != 0)
                {
                    Input.ActionPress(action, action_strength);
                }
                else
                {
                    Input.ActionRelease(action);
                }
            }

            var events = dict["@events"].As<Godot.Collections.Array<Dict>>();
            foreach (Dict event_dict in events)
            {
                var event_record = new EventRecordable();
                event_record.Deserialize(event_dict);
            }
            Input.MouseMode = (Input.MouseModeEnum)dict["MouseMode"].As<ulong>();
        }
    }
}