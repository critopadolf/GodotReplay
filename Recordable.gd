@abstract
class_name IRecordable
extends Node

const INAME = "GD_SCRIPT_IRECORDABLE"
@abstract func UniqueId() -> String
@abstract func Serialize() -> Dictionary
@abstract func Deserialize(dict : Dictionary) -> void
