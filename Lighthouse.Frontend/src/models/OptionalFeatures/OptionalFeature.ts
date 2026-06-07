import { z } from "zod";

export interface IOptionalFeature {
	name: string;
	key: string;
	id: number;
	description: string;
	enabled: boolean;
	isPremium: boolean;
	isPreview: boolean;
}

export const OptionalFeatureSchema = z.object({
	name: z.string(),
	key: z.string(),
	id: z.number(),
	description: z.string(),
	enabled: z.boolean(),
	isPremium: z.boolean(),
	isPreview: z.boolean(),
});
