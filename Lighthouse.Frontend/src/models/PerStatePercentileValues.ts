import type { IPercentileValue } from "./PercentileValue";

export interface IPerStatePercentileValues {
	state: string;
	percentiles: IPercentileValue[];
}
