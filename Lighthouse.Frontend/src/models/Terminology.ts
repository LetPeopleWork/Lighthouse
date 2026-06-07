import { z } from "zod";

export interface ITerminology {
	id: number;
	key: string;
	defaultValue: string;
	description: string;
	value: string;
}

export const TerminologySchema = z.object({
	id: z.number(),
	key: z.string(),
	defaultValue: z.string(),
	description: z.string(),
	value: z.string(),
});
