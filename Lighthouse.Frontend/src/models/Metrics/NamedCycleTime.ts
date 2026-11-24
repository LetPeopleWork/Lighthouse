import { z } from "zod";

export const NamedCycleTimeValueSchema = z.object({
	definitionId: z.number(),
	days: z.number(),
	/** When the Work Item reached the definition's end state; the wire sends it as an ISO string. */
	endDate: z.coerce.date(),
});

export type INamedCycleTimeValue = z.infer<typeof NamedCycleTimeValueSchema>;

export interface INamedCycleTimeDefinition {
	id: number;
	name: string;
	endState: string;
	isValid?: boolean;
}

export interface ICycleTimeDefinition {
	id: number;
	name: string;
	startState: string;
	endState: string;
	isValid?: boolean;
}
