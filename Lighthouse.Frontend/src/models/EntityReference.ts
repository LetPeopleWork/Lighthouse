import { z } from "zod";

export interface IEntityReference {
	id: number;
	name: string;
}

export const EntityReferenceSchema = z.object({
	id: z.number(),
	name: z.string(),
});
