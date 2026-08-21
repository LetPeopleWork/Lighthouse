export interface IDeliveryNote {
	id: number;
	deliveryId: number;
	text: string;
	/** The instant the note was written. Sorting happens on this. */
	createdAt: string;
	/**
	 * The day a reader sees, already reduced on the server in the instance's own zone. Render this
	 * rather than turning `createdAt` into a day here, which lands a day out for readers west of UTC.
	 */
	createdOn: string;
	lastEditedAt?: string | null;
	lastEditedOn?: string | null;
	/** Null when nobody was signed in to name — the note is shown unattributed, never as a placeholder. */
	authorDisplayName?: string | null;
	/**
	 * Whether this reader may correct or withdraw this note. Decided on the server, because the rule
	 * has an awkward case (a note nobody signed) that is not worth having two copies of.
	 */
	canModify?: boolean;
}

export class DeliveryNote implements IDeliveryNote {
	id!: number;
	deliveryId!: number;
	text!: string;
	createdAt!: string;
	createdOn!: string;
	lastEditedAt?: string | null;
	lastEditedOn?: string | null;
	authorDisplayName?: string | null;
	canModify?: boolean;

	static fromBackend(data: IDeliveryNote): DeliveryNote {
		const note = new DeliveryNote();
		note.id = data.id;
		note.deliveryId = data.deliveryId;
		note.text = data.text ?? "";
		note.createdAt = data.createdAt;
		note.createdOn = data.createdOn;
		note.lastEditedAt = data.lastEditedAt ?? null;
		note.lastEditedOn = data.lastEditedOn ?? null;
		note.authorDisplayName = data.authorDisplayName ?? null;
		note.canModify = data.canModify ?? false;
		return note;
	}
}
