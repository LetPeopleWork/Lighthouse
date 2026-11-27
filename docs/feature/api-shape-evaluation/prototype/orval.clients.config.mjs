// Prototype orval configuration for the lighthouse-clients package: plain fetch functions and types,
// no zod, no mocks (the clients use undici/fetch, not axios). INPUT and OUT are set by the run script.
export default {
	lighthouseClients: {
		input: { target: process.env.INPUT },
		output: {
			mode: "tags-split",
			client: "fetch",
			target: `${process.env.OUT}/api`,
			schemas: `${process.env.OUT}/model`,
		},
	},
};
