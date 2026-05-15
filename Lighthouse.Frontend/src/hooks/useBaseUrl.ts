import { useContext, useEffect, useState } from "react";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

export const useBaseUrl = (): string | null => {
	const { systemInfoService } = useContext(ApiServiceContext);
	const [baseUrl, setBaseUrl] = useState<string | null>(null);

	useEffect(() => {
		let cancelled = false;
		const fetchBaseUrl = async () => {
			try {
				const info = await systemInfoService.getSystemInfo();
				if (cancelled) {
					return;
				}
				const fromServer = info.baseUrl;
				setBaseUrl(
					typeof fromServer === "string" && fromServer.length > 0
						? fromServer
						: null,
				);
			} catch {
				if (!cancelled) {
					setBaseUrl(null);
				}
			}
		};

		fetchBaseUrl();

		return () => {
			cancelled = true;
		};
	}, [systemInfoService]);

	return baseUrl;
};
