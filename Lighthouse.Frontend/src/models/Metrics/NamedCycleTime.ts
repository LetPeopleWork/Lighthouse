import { z } from "zod";

export const NamedCycleTimeValueSchema = z.object({
	definitionId: z.number(),
	days: z.number(),
});

export type INamedCycleTimeValue = z.infer<typeof NamedCycleTimeValueSchema>;

export interface INamedCycleTimeDefinition {
	id: number;
	name: string;
}

export interface ICycleTimeDefinition {
	id: number;
	name: string;
	startState: string;
	endState: string;
	isValid?: boolean;
}
