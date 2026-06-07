import { z } from "zod";

export interface SystemInfo {
	os: string;
	runtime: string;
	architecture: string;
	processId: number;
	databaseProvider: string;
	databaseConnection: string | null;
	logPath: string | null;
	authenticationEnabled?: boolean;
	authorizationEnabled?: boolean;
	emergencyAdminSubjects?: string[];
	baseUrl?: string;
	installTimestamp?: string;
}

export const SystemInfoSchema = z.object({
	os: z.string(),
	runtime: z.string(),
	architecture: z.string(),
	processId: z.number(),
	databaseProvider: z.string(),
	databaseConnection: z.string().nullable(),
	logPath: z.string().nullable(),
	authenticationEnabled: z.boolean().optional(),
	authorizationEnabled: z.boolean().optional(),
	emergencyAdminSubjects: z.array(z.string()).optional(),
	baseUrl: z.string().optional(),
	installTimestamp: z
		.string()
		.nullish()
		.transform((value) => value ?? undefined),
});
