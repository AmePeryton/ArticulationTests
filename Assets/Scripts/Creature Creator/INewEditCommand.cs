using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface INewEditCommand
{
	void Execute();
	void Undo();
}

// NOTE: Does not check to see if the ID is free or not, so do that in calling method before creating this
public class NewCommandChangeId : INewEditCommand
{
	public SerializedBodyData body;
	public SerializedBodyPartData bodyPart;
	public int oldId;
	public int newId;
	public List<SerializedBodyPartData> childBodyParts;

	public NewCommandChangeId(SerializedBodyData body, SerializedBodyPartData bodyPart, int newId)
	{
		this.body = body;
		this.bodyPart = bodyPart;
		oldId = bodyPart.id;
		this.newId = newId;

		// Find all other parts that reference this one by ID (currently, just children)
		childBodyParts = body.GetChildren(bodyPart).ToList();
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeId\n( " + oldId + " => " + newId + " )\n";
	}

	public void Execute()
	{
		bodyPart.id = newId;
		foreach (SerializedBodyPartData child in childBodyParts)
		{
			child.parentId = newId;
		}
	}

	public void Undo()
	{
		bodyPart.id = oldId;
		foreach (SerializedBodyPartData child in childBodyParts)
		{
			child.parentId = oldId;
		}
	}
}

public class NewCommandChangeParent : INewEditCommand
{
	public SerializedBodyData body;
	public SerializedBodyPartData bodyPart;
	public int oldParentId;
	public int newParentId;
	public int oldId;

	public NewCommandChangeParent(SerializedBodyData body, SerializedBodyPartData bodyPart, SerializedBodyPartData newParent)
	{
		this.body = body;
		this.bodyPart = bodyPart;
		oldParentId = body.GetParent(bodyPart).id;
		newParentId = newParent.id;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeParent\n( " + oldParentId + " => " + newParentId + " )\n";
	}

	public void Execute()
	{
		bodyPart.parentId = newParentId;
	}

	public void Undo()
	{
		bodyPart.parentId = oldParentId;
	}
}

public class NewCommandChangeSymmetryType : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public SymmetryType oldSymmetryType;
	public SymmetryType newSymmetryType;
	public int oldId;

	public NewCommandChangeSymmetryType(SerializedBodyPartData bodyPart, SymmetryType newSymmetryType)
	{
		this.bodyPart = bodyPart;
		oldSymmetryType = bodyPart.symmetryType;
		this.newSymmetryType = newSymmetryType;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeSymmetryType\n( " + oldSymmetryType + " => " + newSymmetryType + " )\n";
	}

	public void Execute()
	{
		bodyPart.symmetryType = newSymmetryType;
	}

	public void Undo()
	{
		bodyPart.symmetryType = oldSymmetryType;
	}
}

public class NewCommandToggleIsAxial : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public bool newIsAxial; // oldIsaAxial is assumed to be opposite
	public int oldId;

	public NewCommandToggleIsAxial(SerializedBodyPartData bodyPart)
	{
		this.bodyPart = bodyPart;
		newIsAxial = !bodyPart.isAxial;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandToggleIsAxial\n( " + !newIsAxial + " => " + newIsAxial + " )\n";
	}

	public void Execute()
	{
		bodyPart.isAxial = newIsAxial;
	}

	public void Undo()
	{
		bodyPart.isAxial = !newIsAxial;
	}
}

public class NewCommandChangeNumReps : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public int oldNumReps;
	public int newNumReps;
	public int oldId;

	public NewCommandChangeNumReps(SerializedBodyPartData bodyPart, int newNumReps)
	{
		this.bodyPart = bodyPart;
		oldNumReps = bodyPart.numReps;
		this.newNumReps = newNumReps;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeNumReps\n( " + oldNumReps + " => " + newNumReps + " )\n";
	}

	public void Execute()
	{
		bodyPart.numReps = newNumReps;
	}

	public void Undo()
	{
		bodyPart.numReps = oldNumReps;
	}
}

public class NewCommandChangePlaxis : INewEditCommand	// point and direction
{
	public SerializedBodyPartData bodyPart;
	public Vector3 oldPlaxisPoint;
	public Vector3 oldPlaxisDirection;
	public Vector3 newPlaxisPoint;
	public Vector3 newPlaxisDirection;
	public int oldId;

	public NewCommandChangePlaxis(SerializedBodyPartData bodyPart, Vector3 newPlaxisDirection, Vector3 newPlaxisPoint)
	{
		this.bodyPart = bodyPart;
		oldPlaxisDirection = bodyPart.plaxisDirection;
		oldPlaxisPoint = bodyPart.plaxisPoint;
		this.newPlaxisDirection = newPlaxisDirection;
		this.newPlaxisPoint = newPlaxisPoint;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangePlaxis" +
			"\nDirection " + oldPlaxisDirection + " => " + newPlaxisDirection + 
			"\nPoint " + oldPlaxisPoint + " => " + newPlaxisPoint + "\n";
	}

	public void Execute()
	{
		bodyPart.plaxisDirection = newPlaxisDirection;
		bodyPart.plaxisPoint = newPlaxisPoint;
	}

	public void Undo()
	{
		bodyPart.plaxisDirection = oldPlaxisDirection;
		bodyPart.plaxisPoint = oldPlaxisPoint;
	}
}

public class NewCommandChangePosition : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public Vector3 oldPosition;
	public Vector3 newPosition;
	public int oldId;

	public NewCommandChangePosition(SerializedBodyPartData bodyPart, Vector3 newPosition)
	{
		this.bodyPart = bodyPart;
		oldPosition = bodyPart.position;
		this.newPosition = newPosition;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangePosition\n" + oldPosition + " => " + newPosition + "\n";
	}

	public void Execute()
	{
		bodyPart.position = newPosition;
	}

	public void Undo()
	{
		bodyPart.position = oldPosition;
	}
}

public class NewCommandChangeRotation : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public Vector3 oldRotation;
	public Vector3 newRotation;
	public int oldId;

	public NewCommandChangeRotation(SerializedBodyPartData bodyPart, Vector3 newRotation)
	{
		this.bodyPart = bodyPart;
		oldRotation = bodyPart.rotation;
		this.newRotation = newRotation;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeRotation\n" + oldRotation + " => " + newRotation + "\n";
	}

	public void Execute()
	{
		bodyPart.rotation = newRotation;
	}

	public void Undo()
	{
		bodyPart.rotation = oldRotation;
	}
}

public class NewCommandChangeScale : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public Vector3 oldScale;
	public Vector3 newScale;
	public int oldId;

	public NewCommandChangeScale(SerializedBodyPartData bodyPart, Vector3 newScale)
	{
		this.bodyPart = bodyPart;
		oldScale = bodyPart.scale;
		this.newScale = newScale;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeScale\n" + oldScale + " => " + newScale + "\n";
	}

	public void Execute()
	{
		bodyPart.scale = newScale;
	}

	public void Undo()
	{
		bodyPart.scale = oldScale;
	}
}

public class NewCommandChangeBulkOffset : INewEditCommand
{
	public SerializedBodyPartData bodyPart;
	public Vector3 oldBulkOffset;
	public Vector3 newBulkOffset;
	public int oldId;

	public NewCommandChangeBulkOffset(SerializedBodyPartData bodyPart, Vector3 newBulkOffset)
	{
		this.bodyPart = bodyPart;
		oldBulkOffset = bodyPart.bulkOffset;
		this.newBulkOffset = newBulkOffset;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandChangeBulkOffset\n" + oldBulkOffset + " => " + newBulkOffset + "\n";
	}

	public void Execute()
	{
		bodyPart.bulkOffset = newBulkOffset;
	}

	public void Undo()
	{
		bodyPart.bulkOffset = oldBulkOffset;
	}
}

// Assumes the assigned ID has already been checked to make sure it's free
public class NewCommandAddBodyPart : INewEditCommand
{
	public SerializedBodyData body;
	public SerializedBodyPartData newBodyPart;
	public SerializedBodyPartData parent;
	public int oldId;

	public NewCommandAddBodyPart(SerializedBodyData body, SerializedBodyPartData newBodyPart)
	{
		this.body = body;
		this.newBodyPart = newBodyPart;
		parent = body.GetParent(newBodyPart);

		oldId = newBodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandAddBodyPart" +
			"\nParent ( " + parent + " )\n";
	}

	public void Execute()
	{
		body.sBodyParts.Add(newBodyPart);
		body.sBodyPartIndex++;
	}

	public void Undo()
	{
		body.sBodyParts.Remove(newBodyPart);
		body.sBodyPartIndex--;
	}
}

// NOTE: this command ONLY performs actions to delete this specific body part, not its children.
// The children of the deleted part will need to have their parent reassigned or be deleted themselves before this part.
// The calling method is responsible for organizing what happenes with the children and makings commands for it
public class NewCommandDeleteBodyPart : INewEditCommand
{
	public SerializedBodyData body;
	public SerializedBodyPartData bodyPart;
	public int oldId;

	public NewCommandDeleteBodyPart(SerializedBodyData body, SerializedBodyPartData bodyPart)
	{
		this.body = body;
		this.bodyPart = bodyPart;

		oldId = bodyPart.id;
	}

	public override string ToString()
	{
		return "[" + oldId + "] NewCommandDeleteBodyPart\n";
	}

	public void Execute()
	{
		body.sBodyParts.Remove(bodyPart);
	}

	public void Undo()
	{
		body.sBodyParts.Add(bodyPart);
	}
}

public class NewMultiCommand : INewEditCommand
{
	public List<INewEditCommand> subCommands;

	public NewMultiCommand(List<INewEditCommand> subCommands)
	{
		this.subCommands = subCommands;
	}

	public override string ToString()
	{
		string output = "NewMultiCommand\n( " + subCommands.Count + " Subcommands )\n\n";
		output += string.Join("\n", subCommands);
		return output;
	}

	public void Execute()
	{
		foreach (INewEditCommand command in subCommands)
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
}