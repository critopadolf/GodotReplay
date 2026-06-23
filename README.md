# Godot Replay

Record your gameplay and play it back. This is a debugging tool that is not fully featured, feel free to add to it.


### Implementing the IRecordable interface
```c#
public partial class Player : CharacterBody3D, Recordable.IRecordable
{
	public string UniqueId()
	{
		return GetPath();
	}
	public Godot.Collections.Dictionary Serialize()
	{
        var dict = new Godot.Collections.Dictionary
        {
            { "GlobalTransform", GlobalTransform },
            { "Velocity", Velocity },
            
			{ "saved_camera_data.global_pos", saved_camera_data.global_pos },
            { "saved_camera_data.valid", saved_camera_data.valid },
            { "original_capsule_height", original_capsule_height },

            { "head", head.GlobalTransform },
            { "camera_smooth", camera_smooth.GlobalTransform },
            { "camera", camera.GlobalTransform },

            { "current_recoil", current_recoil },
            { "target_recoil", target_recoil },
            { "cam_aligned_wish_dir", cam_aligned_wish_dir },
            { "wish_dir", wish_dir },

			{"health", health },
			{ "max_health", health},
			{"_last_frame_was_on_floor", _last_frame_was_on_floor },
			{"_snapped_to_stairs_last_frame", bool_int(_snapped_to_stairs_last_frame) },
			{"is_crouched", bool_int(is_crouched) },
			{"noclip", bool_int(noclip) },
		};
		return dict;
    }

	public void Deserialize(Godot.Collections.Dictionary dict)
	{
        GlobalTransform = dict["GlobalTransform"].As<Transform3D>();
        Velocity = dict["Velocity"].As<Vector3>();

        saved_camera_data.global_pos = dict["saved_camera_data.global_pos"].As<Vector3>();
        saved_camera_data.valid = dict["saved_camera_data.valid"].As<uint>() > 0;
        original_capsule_height = dict["original_capsule_height"].As<float>();


        head.GlobalTransform = dict["head"].As<Transform3D>();
        camera_smooth.GlobalTransform = dict["camera_smooth"].As<Transform3D>();
        camera.GlobalTransform = dict["camera"].As<Transform3D>();

        current_recoil = dict["current_recoil"].As<Vector2>();
        target_recoil = dict["target_recoil"].As<Vector2>();
        cam_aligned_wish_dir = dict["cam_aligned_wish_dir"].As<Vector3>();
        wish_dir = dict["wish_dir"].As<Vector3>();

        health = dict["health"].As<float>();
        max_health = dict["max_health"].As<float>();
        _last_frame_was_on_floor = dict["_last_frame_was_on_floor"].As<ulong>();
        _snapped_to_stairs_last_frame = dict["_snapped_to_stairs_last_frame"].As<uint>() > 0;
        is_crouched = dict["is_crouched"].As<uint>() > 0;
        noclip = dict["noclip"].As<uint>() > 0;
    }
}
```

### Implementing the IRecordable interface with GDScript
```gdscript
extends Node3D

class MyRecordable extends IRecordable:
	var node : Node3D
	func _init(p_node : Node):
		node = p_node
	func UniqueId():
		return self.node.get_path()
	func Serialize():
		var dict = {
			"position" : node.position
		}
		return dict
	func Deserialize(dict : Dictionary) -> void:
		node.position = dict["position"]
		return

var my_recordable : MyRecordable;
func _ready():
	my_recordable = MyRecordable.new(self)
	set_meta(IRecordable.INAME, my_recordable)

func _process(delta):
	position += Vector3(delta, 0, 0)
```