const GUID_REGEX =
	/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const getWorkItemName = (
	workItemName: string,
	workItemReferenceId: string,
): string => {
	if (workItemName?.toLowerCase().includes("unparented")) {
		return workItemName;
	}

	if (!workItemName || workItemName.trim() === "") {
		return workItemReferenceId;
	}

	if (GUID_REGEX.test(workItemReferenceId)) {
		return workItemName;
	}

	return `${workItemReferenceId}: ${workItemName}`;
};
