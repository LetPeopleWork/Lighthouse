import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Link,
	Paper,
	Stack,
	Tooltip,
	Typography,
} from "@mui/material";
import { useCallback, useContext, useEffect, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useRbac } from "../../../hooks/useRbac";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	IOAuthHealthDto,
	IOAuthHealthMetric,
} from "../../../services/Api/OAuthService";

const EVENT_STORE_PENDING_TOOLTIP =
	"Computing this KPI requires the OAuth event store (Epic #5017). Until that ships, the tile surfaces the stale-RefreshFailed counts only.";

const UPGRADE_COPY =
	"OAuth Health is a Premium feature. Upgrade to Premium to monitor connection KPIs.";

const formatPercentage = (rate: number): string => {
	const pct = rate * 100;
	if (pct >= 100) {
		return "100%";
	}
	return `${pct.toFixed(pct < 10 ? 1 : 0)}%`;
};

const MetricValue = ({ metric }: { metric: IOAuthHealthMetric }) => {
	if (metric.value !== null) {
		return (
			<Typography variant="body2" sx={{ fontWeight: 600 }}>
				{formatPercentage(metric.value)}
			</Typography>
		);
	}

	return (
		<Tooltip title={EVENT_STORE_PENDING_TOOLTIP} arrow>
			<Typography
				variant="body2"
				color="text.secondary"
				aria-label={`Pending — ${EVENT_STORE_PENDING_TOOLTIP}`}
				sx={{ fontStyle: "italic" }}
			>
				Pending (Epic #5017)
			</Typography>
		</Tooltip>
	);
};

const KpiRow = ({
	label,
	children,
}: {
	label: string;
	children: React.ReactNode;
}) => (
	<Box
		sx={{
			display: "flex",
			justifyContent: "space-between",
			alignItems: "center",
			py: 0.5,
		}}
	>
		<Typography variant="body2">{label}</Typography>
		{children}
	</Box>
);

const StaleCountText = ({
	count24h,
	count7d,
}: {
	count24h: number;
	count7d: number;
}) => {
	if (count24h === 0 && count7d === 0) {
		return (
			<Typography variant="body2" sx={{ fontWeight: 600 }} color="success.main">
				All connections healthy
			</Typography>
		);
	}

	const message = `${count24h} connections require reconnect (24h) / ${count7d} (7d)`;
	return (
		<Typography variant="body2" sx={{ fontWeight: 600 }} color="warning.main">
			{message}
		</Typography>
	);
};

const UpgradeAffordance = () => (
	<Paper
		variant="outlined"
		sx={{ p: 2, mb: 2 }}
		data-testid="oauth-health-tile"
	>
		<Typography variant="h6" sx={{ mb: 1 }}>
			OAuth Health
		</Typography>
		<Alert severity="info" data-testid="oauth-health-tile-upgrade-affordance">
			<Typography variant="body2">
				{UPGRADE_COPY}{" "}
				<Link component={RouterLink} to="/settings/license">
					View license options
				</Link>
			</Typography>
		</Alert>
	</Paper>
);

const OAuthHealthTile = () => {
	const { isSystemAdmin } = useRbac();
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;
	const { oauthService } = useContext(ApiServiceContext);

	const [health, setHealth] = useState<IOAuthHealthDto | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [forbidden, setForbidden] = useState(false);

	const shouldFetch = isSystemAdmin && canUsePremiumFeatures;

	const loadHealth = useCallback(async () => {
		setIsLoading(true);
		setError(null);
		try {
			const dto = await oauthService.getHealth();
			setHealth(dto);
		} catch (err: unknown) {
			if (err instanceof ApiError && err.code === 403) {
				setForbidden(true);
			} else {
				setError("Failed to load OAuth health. Please try again.");
			}
		} finally {
			setIsLoading(false);
		}
	}, [oauthService]);

	useEffect(() => {
		if (!shouldFetch) {
			return;
		}
		loadHealth();
	}, [shouldFetch, loadHealth]);

	if (!isSystemAdmin) {
		return null;
	}

	if (!canUsePremiumFeatures || forbidden) {
		return <UpgradeAffordance />;
	}

	return (
		<Paper
			variant="outlined"
			sx={{ p: 2, mb: 2 }}
			data-testid="oauth-health-tile"
		>
			<Typography variant="h6" sx={{ mb: 1 }}>
				OAuth Health
			</Typography>
			{isLoading && (
				<Box
					sx={{
						display: "flex",
						justifyContent: "center",
						py: 2,
					}}
				>
					<CircularProgress size={24} />
				</Box>
			)}
			{error && !isLoading && (
				<Alert
					severity="warning"
					action={
						<Button color="inherit" size="small" onClick={loadHealth}>
							Retry
						</Button>
					}
					data-testid="oauth-health-tile-error"
				>
					{error}
				</Alert>
			)}
			{health && !isLoading && !error && (
				<Stack
					divider={
						<Box sx={{ borderTop: "1px solid", borderColor: "divider" }} />
					}
				>
					<KpiRow label="Setup success rate (30d)">
						<MetricValue metric={health.setupSuccessRate30d} />
					</KpiRow>
					<KpiRow label="Refresh success rate (7d)">
						<MetricValue metric={health.refreshSuccessRate7d} />
					</KpiRow>
					<KpiRow label="Stale RefreshFailed connections">
						<StaleCountText
							count24h={health.staleRefreshFailedCount24h}
							count7d={health.staleRefreshFailedCount7d}
						/>
					</KpiRow>
				</Stack>
			)}
		</Paper>
	);
};

export default OAuthHealthTile;
