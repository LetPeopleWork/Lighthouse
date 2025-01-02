import { CircularProgress } from "@mui/material";
import type React from "react";

interface LoadingAnimationProps {
	isLoading: boolean;
	hasError: boolean;
	children: React.ReactNode;
}

const LoadingAnimation: React.FC<LoadingAnimationProps> = ({
	isLoading,
	hasError = false,
	children,
}) => {
	if (hasError) {
		return (
			<div data-testid="loading-animation-error-message">
				Error loading data. Please try again later.
			</div>
		);
	}

	if (isLoading) {
		return (
			<CircularProgress data-testid="loading-animation-progress-indicator" />
		);
	}

	return <>{children}</>;
};

export default LoadingAnimation;
