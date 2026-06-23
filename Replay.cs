using Godot;
using System;
using System.Collections.Generic;
using Dict = Godot.Collections.Dictionary;
using Recordable;

/// <summary>
/// <para>Records inputs and states of Nodes which implement the `Recordable.IRecordable` interface.</para>
/// <para>Actions: Save with `debug_save_replay`, Playback with `debug_load_replay`, and Pause with `debug_pause_replay`</para>
/// </summary>
public partial class Replay : Node
{
    const string gd_script_member = "GD_SCRIPT_IRECORDABLE";
    /// <summary>
    /// <para>The previous number of seconds to record when the</para>
    /// <para>`debug_save_replay` action is pressed</para>
    /// </summary>
    [Export] uint record_seconds = 5;
    /// <summary>
    /// <para>Mask for including different types in the recording</para>
    /// <para>Setting the Node flag is not recommended. It will record the GlobalTransform of all Node3D/Node2D</para>
    /// </summary>
    [Flags]
    public enum UseRecordables
    {
        IRecordable = 1 << 1,
        RigidBody = 1 << 2,
        CharacterBody = 1 << 3,
        Node = 1 << 4,
    }
    [Export]
    public UseRecordables use_recordables = UseRecordables.IRecordable;

    /// <summary>
    /// <para>Loop the recording after playback when `True`</para>
    /// <para>recording will pause when `False`</para>
    /// </summary>
    [Export] bool loop = false;
    /// <summary>
    /// <para>When `True`, starts the recording from an initial state, then only plays back actions and implemented events</para>
    /// <para>When `False`, plays back all states as they are saved</para>
    /// </summary>
    [Export] bool use_only_initial_state = true;
    /// <summary>
    /// <para> Stores a singular file in this directory, replaced whenever the `debug_save_replay` action is pressed</para>
    /// </summary>
    [Export(PropertyHint.Dir)] string file_directory;

    List<IRecordable> recordables;
    InputRecordable input_recordable;

    enum State
    {
        Recording,
        Playback,
        Paused,
        None
    };

    State state = State.Recording;
    struct FrameData
    {
        public Dict dict;
        public ulong ticks;
    }
    LinkedList<FrameData> recording;
    LinkedListNode<FrameData> current_frame;
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        recording = new LinkedList<FrameData>();
        recordables = [];
        input_recordable = new InputRecordable();
        recordables.Add(input_recordable);
        Stack<Node> nodes = new();
        nodes.Push(GetTree().CurrentScene);
        while (nodes.Count > 0)
        {
            Node node = nodes.Pop();
            if((use_recordables & UseRecordables.IRecordable) > 0 && node.HasMeta(gd_script_member))
            {
                Node recordable_node = node.GetMeta(gd_script_member).As<Node>();
                recordables.Add(new GDScriptRecordable(recordable_node));
            }
            else if ((use_recordables & UseRecordables.IRecordable) > 0 && node is IRecordable)
            {
                recordables.Add(node as IRecordable);
            }
            else if((use_recordables & UseRecordables.RigidBody) > 0 && node is RigidBody3D)
            {
                recordables.Add(new RigidBody3DRecordable(node as RigidBody3D));
            }
            else if((use_recordables & UseRecordables.CharacterBody) > 0 && node is CharacterBody3D)
            {
                recordables.Add(new CharacterBody3DRecordable(node as CharacterBody3D));
            }
            else if((use_recordables & UseRecordables.Node) > 0 && node is Node3D)
            {
                recordables.Add(new Node3DRecordable(node as Node3D));
            }
            else if ((use_recordables & UseRecordables.RigidBody) > 0 && node is RigidBody2D)
            {
                recordables.Add(new RigidBody2DRecordable(node as RigidBody2D));
            }
            else if ((use_recordables & UseRecordables.CharacterBody) > 0 && node is CharacterBody2D)
            {
                recordables.Add(new CharacterBody2DRecordable(node as CharacterBody2D));
            }
            else if ((use_recordables & UseRecordables.Node) > 0 && node is Node2D)
            {
                recordables.Add(new Node2DRecordable(node as Node2D));
            }

            foreach (Node child in node.GetChildren())
            {
                nodes.Push(child);
            }
        }
    }

    string FilePath()
    {
        if (file_directory == null)
            throw new Exception("file directory not set");
        return file_directory + "recording.rsc";
    }

    void Save()
    {
        string filepath = FilePath();
        var data = new Godot.Collections.Array<Dict>();
        foreach(FrameData frame_data in recording)
        {
            data.Add(frame_data.dict);
        }
        var file = FileAccess.Open(filepath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            throw new Exception("Failed open file `" + filepath + "`");
        }
        file.StoreVar(data);
        file.Close();
    }

    void Load()
    {
        GetTree().Paused = false;
        string filepath = FilePath();
        var file = FileAccess.Open(filepath, FileAccess.ModeFlags.Read);
        if(file is null)
        {
            return;
        }
        recording = new LinkedList<FrameData>();
        var data = file.GetVar().As<Godot.Collections.Array<Dict>>();
        foreach (Dict dict in data)
        {
            var frame_data = new FrameData
            {
                dict = dict
            };
            recording.AddLast(frame_data);
        }
        current_frame = recording.First;
        state = State.Playback;
    }

    private void RecordProcess(double delta)
    {
        if (Input.IsActionJustPressed("debug_save_replay"))
        {
            Save();
            state = State.Recording;
            return;
        }

        var frame_data = new FrameData
        {
            dict = [],
            ticks = Time.GetTicksMsec()
        };
        foreach (IRecordable record in recordables)
        {
            frame_data.dict[record.UniqueId()] = record.Serialize();
        }
            
        recording.AddLast(frame_data);

        LinkedListNode<FrameData> frame_node = recording.First;
        LinkedListNode<FrameData> frame_node_next;
        while (frame_node is not null)
        {
            ulong elapsed = frame_data.ticks - frame_node.ValueRef.ticks;
            if(elapsed >= (record_seconds * 1000))
            {
                frame_node_next = frame_node.Next;
                recording.Remove(frame_node);
                frame_node = frame_node_next;
            }
            else
            {
                break;
            }
        }
        input_recordable.ClearFrame();
    }
    private void PlaybackProcess(double delta)
    {
        if (Input.IsActionJustPressed("debug_save_replay"))
        {
            state = State.Recording;
            GetTree().Paused = false;
            current_frame = null;
            recording.Clear();
            return;
        }
        if(Input.IsActionJustPressed("debug_pause_replay"))
        {
            GetTree().Paused = true;
            state = State.Paused;
            return;
        }
        if (current_frame is null)
        {
            if (loop)
            {
                current_frame = recording.First;
            }
            else
            {
                GetTree().Paused = true;
                state = State.Paused;
            }
            return;
        }
        Dict dict = current_frame.ValueRef.dict;
        if (!use_only_initial_state || current_frame == recording.First)
        {
            foreach (IRecordable record in recordables)
            {
                record.Deserialize(dict[record.UniqueId()].As<Dict>());
            }
        }
        else
        {
            input_recordable.Deserialize(dict[input_recordable.UniqueId()].As<Dict>());
        }
        current_frame = current_frame.Next;
    }

    private void PausedProcess(double delta)
    {
        if (Input.IsActionJustPressed("debug_pause_replay"))
        {
            if (current_frame is not null)
            {
                GetTree().Paused = false;
                state = State.Playback;
            }
        }
        else if (Input.IsActionJustPressed("debug_save_replay"))
        {
            state = State.Recording;
            GetTree().Paused = false;
            current_frame = null;
            recording.Clear();
            return;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (state is State.Recording)
        {
            input_recordable.Event(@event);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // This will not filter InputEventMouseMotion.Relative correctly
        if ((state is State.Playback || state is State.Paused) && EventRecordable.filter(@event))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        switch(state)
        {
        case (State.Recording):
        {
            RecordProcess(delta);
            break;
        }
        case (State.Playback):
        {
            PlaybackProcess(delta);
            break;
        }
        case (State.Paused):
        {
            PausedProcess(delta);
            break;
        }
        }
        if (Input.IsActionJustPressed("debug_load_replay"))
        {
            Load();
        }
    }
}
