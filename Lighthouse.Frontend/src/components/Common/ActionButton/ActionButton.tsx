import { Button, CircularProgress, useTheme } from "@mui/material";
import type React from "react";
import { useEffect, useRef, useState } from "react";

type ButtonVariant = "text" | "outlined" | "contained";

interface ActionButtonProps {
	buttonText: string;
	onClickHandler: () => Promise<void>;
	buttonVariant?: ButtonVariant;
	disabled?: boolean;
	maxHeight?: string;
	minWidth?: string;
	externalIsWaiting?: boolean;
	startIcon?: React.ReactNode;
	fullWidth?: boolean;
	color?: "primary" | "secondary" | "success" | "error" | "info" | "warning";
	autoFocus?: boolean;
	/** The id of the element that explains why the button is disabled. */
	describedBy?: string;
}

const ActionButton: React.FC<ActionButtonProps> = ({
	buttonText,
	onClickHandler,
	buttonVariant = "contained",
	disabled = false,
	maxHeight,
	minWidth,
	externalIsWaiting = false,
	startIcon,
	fullWidth = false,
	color = "primary",
	autoFocus = false,
	describedBy,
}) => {
	const [internalIsWaiting, setInternalIsWaiting] = useState<boolean>(false);
	const isMounted = useRef(true);

	useEffect(() => {
		isMounted.current = true;
		return () => {
			isMounted.current = false;
		};
	}, []);

	const theme = useTheme();

	const handleClick = async () => {
		setInternalIsWaiting(true);

		try {
			await Promise.all([
				onClickHandler(),
				// At least switch to waiting state for 300ms to avoid flickering
				new Promise((resolve) => setTimeout(resolve, 300)),
			]);
		} finally {
			// The minimum wait can outlive the button: the click may navigate away or close its dialog.
			if (isMounted.current) setInternalIsWaiting(false);
		}
	};

	const isWaiting = internalIsWaiting || externalIsWaiting;

	return (
		<Button
			variant={buttonVariant}
			onClick={handleClick}
			disabled={disabled || isWaiting}
			aria-busy={isWaiting}
			aria-describedby={describedBy}
			startIcon={!isWaiting && startIcon}
			fullWidth={fullWidth}
			color={color}
			autoFocus={autoFocus}
			sx={{
				position: "relative",
				display: "flex",
				alignItems: "center",
				justifyContent: "center",
				maxHeight: maxHeight,
				minHeight: "36px",
				minWidth: minWidth,
				py: 1,
				px: 2,
				borderRadius: 2,
				textTransform: "none",
				fontWeight: 500,
				boxShadow: theme.shadows[4],
				"&:active": {
					transform: "translateY(0px)",
				},
				"& .MuiButton-startIcon": {
					marginRight: 1,
				},
				opacity: isWaiting ? 0.85 : 1,
			}}
		>
			{isWaiting ? (
				<>
					<CircularProgress
						size={24}
						thickness={4}
						sx={{
							color:
								buttonVariant === "contained"
									? theme.palette.common.white
									: `${theme.palette[color].main}`,
							marginRight: 1,
						}}
					/>
					{buttonText}
				</>
			) : (
				buttonText
			)}
		</Button>
	);
};

export default ActionButton;
