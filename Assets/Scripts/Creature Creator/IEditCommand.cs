using System.Collections.Generic;
using UnityEngine;

public interface IEditCommand
{
	void Execute();
	void Undo();
	void LateExecute();
}

public class CommandChangeSymmetry : IEditCommand
{
	public BodyController bodyController;
	public SymmetryType oldSymmetryType;
	public SymmetryType newSymmetryType;

	public CommandChangeSymmetry(BodyController bodyController, SymmetryType newSymmetryType)
	{
		this.bodyController = bodyController;
		oldSymmetryType = bodyController.data.symmetryType;
		this.newSymmetryType = newSymmetryType;
	}

	public override string ToString()
	{
		return "CommandChangeSymmetry: (" + oldSymmetryType.ToString() + " ==> " + newSymmetryType.ToString() + ")";
	}

	public void Execute()
	{
		bodyController.data.symmetryType = newSymmetryType;

	}

	public void Undo()
	{
		bodyController.data.symmetryType = oldSymmetryType;
	}

	public void LateExecute()
	{
		bodyController.UpdateSymmetry();
	}
}

public class CommandChangeNumSegments : IEditCommand
{
	public BodyController bodyController;
	public int oldNumSegments;
	public int newNumSegments;

	public CommandChangeNumSegments(BodyController bodyController, int newNumSegments)
	{
		this.bodyController = bodyController;
		oldNumSegments = bodyController.data.numSegments;
		this.newNumSegments = newNumSegments;
	}

	public override string ToString()
	{
		return "CommandChangeNumSegments: (" + oldNumSegments.ToString() + " ==> " + newNumSegments.ToString() + ")";
	}

	public void Execute()
	{
		bodyController.data.numSegments = newNumSegments;
	}

	public void Undo()
	{
		bodyController.data.numSegments = oldNumSegments;
	}

	public void LateExecute()
	{
		bodyController.UpdateSymmetry();
	}
}

public class CommandAddPart : IEditCommand
{
	public BodyController bodyController;

	public BodyPartData data;

	public CommandAddPart(BodyController bodyController, BodyPartData data)
	{
		this.bodyController = bodyController;

		this.data = data;
	}

	public override string ToString()
	{
		return "CommandAddPart: (" + data.parentId.ToString() + " ++ " + data.id + ")";
	}

	public void Execute()
	{
		bodyController.AddBodyPartData(data);
	}

	public void Undo()
	{
		bodyController.DeleteBodyPart(data);
	}

	public void LateExecute()
	{
	}
}

public class CommandDeletePart : IEditCommand
{
	public BodyController bodyController;

	public BodyPartData data;

	public CommandDeletePart(BodyController bodyController, BodyPartData data)
	{
		this.bodyController = bodyController;

		this.data = data;
	}

	public override string ToString()
	{
		return "CommandDeletePart: (" + data.id.ToString() + ")";
	}

	public void Execute()
	{
		bodyController.DeleteBodyPart(data);
	}

	public void Undo()
	{
		bodyController.AddBodyPartData(data);
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeId : IEditCommand
{
	public BodyController bodyController;

	public BodyPartData data;
	public int oldId;
	public int newId;

	public CommandChangeId(BodyController bodyController, BodyPartData data, int newId)
	{
		this.bodyController = bodyController;

		this.data = data;
		oldId = data.id;
		this.newId = newId;
	}

	public override string ToString()
	{
		return "CommandChangeId: (" + oldId.ToString() + " ==> " + newId.ToString() + ")";
	}

	public void Execute()
	{
		bodyController.ChangeBodyPartId(data, newId);
	}

	public void Undo()
	{
		bodyController.ChangeBodyPartId(data, oldId);
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeParent : IEditCommand
{
	public BodyController bodyController;
	public BodyPartData data;
	public int oldParentId;
	public int newParentId;

	public CommandChangeParent(BodyController bodyController, BodyPartData data, int newParentId)
	{
		this.bodyController = bodyController;
		this.data = data;
		oldParentId = data.parentId;
		this.newParentId = newParentId;
	}

	public override string ToString()
	{
		return "CommandChangeParent: (" + data.id.ToString() + ", " + 
			oldParentId.ToString() + " ==> " + newParentId.ToString() + ")";
	}

	public void Execute()
	{
		BodyPartController controller = bodyController.bodyPartsDict[data.id];
		BodyPartController parentController = bodyController.bodyPartsDict[newParentId];
		controller.ChangeParent(parentController);
	}

	public void Undo()
	{
		BodyPartController controller = bodyController.bodyPartsDict[data.id];
		BodyPartController parentController = bodyController.bodyPartsDict[oldParentId];
		controller.ChangeParent(parentController);
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeName : IEditCommand
{
	public BodyPartData data;
	public string oldName;
	public string newName;

	public CommandChangeName(BodyPartData data, string newName)
	{
		this.data = data;
		oldName = data.name;
		this.newName = newName;
	}

	public override string ToString()
	{
		return "CommandChangeName: (" + data.id.ToString() + ", " + oldName + " ==> " + newName + ")";
	}

	public void Execute()
	{
		data.name = newName;
	}

	public void Undo()
	{
		data.name = oldName;
	}

	public void LateExecute()
	{
	}
}

public class CommandMove : IEditCommand
{
	public BodyPartData data;
	public Vector3 oldPosition;
	public Vector3 newPosition;

	public CommandMove(BodyPartData data, Vector3 newPosition)
	{
		this.data = data;
		oldPosition = data.position;
		this.newPosition = newPosition;
	}

	public override string ToString()
	{
		return "CommandMove: (" + data.id.ToString() + ", " + oldPosition.ToString() + " ==> " + newPosition.ToString() + ")";
	}

	public void Execute()
	{
		data.position = newPosition;
	}

	public void Undo()
	{
		data.position = oldPosition;
	}

	public void LateExecute()
	{
	}
}

public class CommandRotate : IEditCommand
{
	public BodyPartData data;
	public Vector3 oldRotation;
	public Vector3 newRotation;

	public CommandRotate(BodyPartData data, Vector3 newRotation)
	{
		this.data = data;
		oldRotation = data.rotation;
		this.newRotation = newRotation;
	}

	public override string ToString()
	{
		return "CommandRotate: (" + data.id.ToString() + ", " + oldRotation.ToString() + " ==> " + newRotation.ToString() + ")";
	}

	public void Execute()
	{
		data.rotation = newRotation;
	}

	public void Undo()
	{
		data.rotation = oldRotation;
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeLength : IEditCommand
{
	public BodyPartData data;
	public float oldLength;
	public float newLength;

	public CommandChangeLength(BodyPartData data, float newLength)
	{
		this.data = data;
		oldLength = data.length;
		this.newLength = newLength;
	}

	public override string ToString()
	{
		return "CommandChangeLength: (" + data.id.ToString() + ", " + oldLength.ToString() + " ==> " + newLength.ToString() + ")";
	}

	public void Execute()
	{
		data.length = newLength;
	}

	public void Undo()
	{
		data.length = oldLength;
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeScale : IEditCommand
{
	public BodyPartData data;
	public Vector3 oldScale;
	public Vector3 newScale;

	public CommandChangeScale(BodyPartData data, Vector3 newScale)
	{
		this.data = data;
		oldScale = data.scale;
		this.newScale = newScale;
	}

	public override string ToString()
	{
		return "CommandChangeScale: (" + data.id.ToString() + ", " + oldScale.ToString() + " ==> " + newScale.ToString() + ")";
	}

	public void Execute()
	{
		data.scale = newScale;
	}

	public void Undo()
	{
		data.scale = oldScale;
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeBulkOffset : IEditCommand
{
	public BodyPartData data;
	public Vector3 oldBulkOffset;
	public Vector3 newBulkOffset;

	public CommandChangeBulkOffset(BodyPartData data, Vector3 newBulkOffset)
	{
		this.data = data;
		oldBulkOffset = data.bulkOffset;
		this.newBulkOffset = newBulkOffset;
	}

	public override string ToString()
	{
		return "CommandChangeBulkOffset: (" + data.id.ToString() + ", " + oldBulkOffset.ToString() + " ==> " + newBulkOffset.ToString() + ")";
	}

	public void Execute()
	{
		data.bulkOffset = newBulkOffset;
	}

	public void Undo()
	{
		data.bulkOffset = oldBulkOffset;
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeJointLimits : IEditCommand
{
	public BodyPartData data;
	public Vector3[] oldJointLimits;
	public Vector3[] newJointLimits;

	public CommandChangeJointLimits(BodyPartData data, Vector3[] newJointLimits)
	{
		this.data = data;
		oldJointLimits = new Vector3[] { data.jointLimits[0], data.jointLimits[1] };
		this.newJointLimits = new Vector3[] { newJointLimits[0], newJointLimits[1] };
	}

	public override string ToString()
	{
		return "CommandChangeJointLimits: (" + data.id.ToString() + ", [" + 
			oldJointLimits[0].ToString() + ", " + oldJointLimits[1].ToString() + "] ==> [" +
			newJointLimits[0].ToString() + ", " + newJointLimits[1].ToString() + "])";
	}

	public void Execute()
	{
		data.jointLimits = new Vector3[] { newJointLimits[0], newJointLimits[1] };
	}

	public void Undo()
	{
		data.jointLimits = new Vector3[] { oldJointLimits[0], oldJointLimits[1] };
	}

	public void LateExecute()
	{
	}
}

public class CommandChangeCentered : IEditCommand
{
	public BodyPartData data;
	public bool oldIsCentered;
	public bool newIsCentered;

	public CommandChangeCentered(BodyPartData data, bool newIsCentered)
	{
		this.data = data;
		oldIsCentered = data.isCentered;
		this.newIsCentered = newIsCentered;
	}

	public override string ToString()
	{
		return "CommandChangeCentered: (" + data.id.ToString() + ", " + newIsCentered + ")";
	}

	public void Execute()
	{
		data.isCentered = newIsCentered;
	}

	public void Undo()
	{
		data.isCentered = oldIsCentered;
	}

	public void LateExecute()
	{
		BodyController bodyController = CreatureCreatorUI.instance.bodyController;
		BodyPartController partController = CreatureCreatorUI.instance.bodyController.bodyPartsDict[data.id];
		bodyController.UpdateClones(partController);
	}
}

// Split up mirror parts into independent parts
public class CommandBreakClone : IEditCommand
{
	public BodyPartData data;
	public BodyPartData cloneData;

	public CommandBreakClone(BodyPartData data, BodyPartData cloneData)
	{
		this.data = data;
		this.cloneData = cloneData;
	}

	public override string ToString()
	{
		return "CommandBreakClone: (" + data.id.ToString() + ", " + cloneData.id.ToString() + ")";
	}

	public void Execute()
	{
	}

	public void Undo()
	{
	}

	public void LateExecute()
	{
	}
}

public class MultiCommand : IEditCommand
{
	public List<IEditCommand> subCommands;

	public MultiCommand(List<IEditCommand> subCommands)
	{
		this.subCommands = subCommands;
	}

	public override string ToString()
	{
		string output = "MultiCommand: (\n\t";
		output += string.Join(", \n\t", subCommands);
		output += "\n)";
		return output;
	}

	public void Execute()
	{
		foreach (IEditCommand command in subCommands)
		{
			command.Execute();
		}
	}

	public void Undo()
	{
		// Undo in reverse order
		for (int i = subCommands.Count - 1; i >= 0; i--)
		{
			subCommands[i].Undo();
		}
	}

	public void LateExecute()
	{
		foreach (IEditCommand command in subCommands)
		{
			command.LateExecute();
		}
	}
}