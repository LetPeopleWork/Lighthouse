import type { Query, QueryKey } from "@tanstack/react-query";
import type { PageFreshAnswer } from "./PageFreshAnswer";
import type { PageFreshFailure } from "./PageFreshFailure";

export type PageFreshQuery<T> = Query<
	PageFreshAnswer<T>,
	PageFreshFailure,
	PageFreshAnswer<T>,
	QueryKey
>;
