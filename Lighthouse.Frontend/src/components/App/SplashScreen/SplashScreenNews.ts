/** A quip shown while the backend is starting up. */
export const loadingMessages: readonly string[] = [
	"Parsing the Planning Excel...",
	"Shuffling the poker cards...",
	"Normalizing throughput distribution...",
	"Making all Work Items the same Size...",
	"Ensuring nobodies feelings get hurt by the Forecasts...",
	"Thinking about why I chose 3 in the confidence vote...",
	"Creating the magic formula to convert Story Points into hours...",
];

/** Bite-sized flow-metric facts and tips. */
export const tips: readonly string[] = [
	"We have a documentation that is quite helpful - you should check it out at https://docs.lighthouse.letpeople.work",
	"Grasshopper Club Zurich is the most successful football club in Switzerland as well as the oldest team in Zuirich, founded in 1886.",
	"Ron Jeffries, the creator of Story Points, thinks they were a mistake.",
	"Every lighthouse has a unique light signature — a specific pattern of flashes and pauses — so sailors can identify which lighthouse they're seeing in the dark.",
	"The world's tallest lighthouse is the Jeddah Light in Saudi Arabia, standing at 133 metres (436 ft)",
	"FC Barcelona was founded in 1899 by a Swiss man, Joan Gamper (born Hans Gamper).",
	"It's a major problem in project management: if three tasks each have a 50% chance of finishing on time, the probability that all three finish on time is only 12.5%",
];

export interface Contributor {
	name: string;
	/** Personal website, GitHub profile, or LinkedIn URL. */
	url: string;
}

/** People and groups who have contributed to Lighthouse. */
export const contributors: readonly Contributor[] = [
	{
		name: "Lorenzo Santoro",
		url: "https://www.linkedin.com/in/lorenzo-santoro-57172626/",
	},
	{
		name: "Chris Graves",
		url: "https://www.linkedin.com/in/chris-graves-23455ab8/",
	},
	{
		name: "Agnieszka Reginek",
		url: "https://www.linkedin.com/in/agnieszka-reginek/",
	},
	{
		name: "Hendra Gunawan",
		url: "https://www.linkedin.com/in/hendragunawan823/",
	},
	{
		name: "Gonzalo Mendez",
		url: "https://www.linkedin.com/in/gonzalo-mendez-nz/",
	},
	{ name: "Gabor Bittera", url: "https://www.linkedin.com/in/gaborbittera/" },
	{
		name: "Mihajlo Vilajić",
		url: "https://www.linkedin.com/in/mihajlo-v-6804ba162/",
	},
	{ name: "Myriam Greger", url: "https://www.linkedin.com/in/myriam-greger/" },
];

/** Returns a uniformly random element from the given array. */
export function pickRandom<T>(items: readonly T[]): T {
	return items[Math.floor(Math.random() * items.length)];
}
