import { useCallback, useContext, useEffect, useState } from "react";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IGlobalUpdateStatus } from "../services/UpdateSubscriptionService";

interface UseUpdateAllResult {
	handleUpdateAll: () => Promise<void>;
	globalUpdateStatus: IGlobalUpdateStatus;
	hasError: boolean;
}

export const useUpdateAll = (): UseUpdateAllResult => {
	const [globalUpdateStatus, setGlobalUpdateStatus] =
		useState<IGlobalUpdateStatus>({
			hasActiveUpdates: false,
			activeCount: 0,
		});
	const [hasError, setHasError] = useState(false);

	const { portfolioService, teamService, updateSubscriptionService } =
		useContext(ApiServiceContext);

	const fetchGlobalUpdateStatus = useCallback(async () => {
		try {
			const status = await updateSubscriptionService.getGlobalUpdateStatus();
			setGlobalUpdateStatus(status);
		} catch (error) {
			console.error("Error fetching global update status:", error);
		}
	}, [updateSubscriptionService]);

	useEffect(() => {
		fetchGlobalUpdateStatus();

		const setupGlobalSubscription = async () => {
			try {
				await updateSubscriptionService.subscribeToAllUpdates(() => {
					// When any update happens, fetch the latest status
					fetchGlobalUpdateStatus();
				});
			} catch (error) {
				console.error("Error setting up global update subscription:", error);
			}
		};

		setupGlobalSubscription();

		return () => {
			updateSubscriptionService.unsubscribeFromAllUpdates().catch((error) => {
				console.error("Error cleaning up global update subscription:", error);
			});
		};
	}, [updateSubscriptionService, fetchGlobalUpdateStatus]);

	const handleUpdateAll = useCallback(async () => {
		try {
			setHasError(false);
			// Don't manually set status - let the backend handle it and we'll get updates via subscriptions
			await Promise.all([
				teamService.updateAllTeamData(),
				portfolioService.refreshFeaturesForAllPortfolios(),
			]);
			// Immediately fetch status after triggering updates
			fetchGlobalUpdateStatus();
		} catch (error) {
			console.error("Error updating all teams and portfolios:", error);
			setHasError(true);
		}
	}, [teamService, portfolioService, fetchGlobalUpdateStatus]);

	return {
		handleUpdateAll,
		globalUpdateStatus,
		hasError,
	};
};
