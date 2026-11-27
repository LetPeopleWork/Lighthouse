// Prototype orval configuration (orval 8.48.0). Run from a scratch folder that has orval installed:
//   npx orval --config <this file>
// INPUT and OUT are set by the run script so the same config serves the frontend and the clients.
const input = process.env.INPUT;
const out = process.env.OUT;

export default {
	// 1. zod schemas for every operation's answer (and params/bodies), plus their z.infer types.
	lighthouseZod: {
		input: { target: input },
		output: {
			mode: "tags-split",
			client: "zod",
			target: `${out}/zod`,
			fileExtension: ".zod.ts",
			override: {
				// Without this, date-time fields stay validated strings (zod.iso.datetime) and "coerce" is ignored.
				useDates: true,
				zod: {
					generate: { param: true, query: true, header: false, body: true, response: true },
					// ISO strings from the wire become Date objects, as the hand-written schemas do.
					coerce: { response: ["date"] },
					dateTimeOptions: { offset: true },
				},
			},
		},
	},
	// 2. TS types + axios functions + MSW handlers with Faker data.
	lighthouseClient: {
		input: { target: input },
		output: {
			mode: "tags-split",
			client: "axios-functions",
			target: `${out}/client`,
			schemas: `${out}/model`,
			mock: { generators: [{ type: "msw", delay: false }] },
		},
	},
};
