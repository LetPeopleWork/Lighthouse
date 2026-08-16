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
	// Whose encryption key this instance is on, and where it is kept. Absent unless the caller is a
	// System Administrator - the response leaves it off the wire entirely rather than sending it empty,
	// so there is nothing here to tell a viewer that something was withheld from them.
	encryption?: string;
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
	encryption: z.string().optional(),
	baseUrl: z.string().optional(),
	installTimestamp: z
		.string()
		.nullish()
		.transform((value) => value ?? undefined),
});
