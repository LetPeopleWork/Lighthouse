import { Button, CircularProgress } from "@mui/material";
import type React from "react";
import { useState } from "react";

type ButtonVariant = "text" | "outlined" | "contained";

interface ActionButtonProps {
	buttonText: string;
	onClickHandler: () => Promise<void>;
	buttonVariant?: ButtonVariant;
	disabled?: boolean;
	maxHeight?: string;
	externalIsWaiting?: boolean;
}

const ActionButton: React.FC<ActionButtonProps> = ({
	buttonText,
	onClickHandler,
	buttonVariant = "contained",
	disabled = false,
	maxHeight,
	externalIsWaiting = false,
}) => {
	const [internalIsWaiting, setInternalIsWaiting] = useState<boolean>(false);

	const handleClick = async () => {
		setInternalIsWaiting(true);
		await Promise.all([
			onClickHandler(),

			// At least switch to waiting state for 300ms to avoid flickering
			new Promise((resolve) => setTimeout(resolve, 300)),
		]);

		setInternalIsWaiting(false);
	};

	const isWaiting = internalIsWaiting || externalIsWaiting;

	return (
		<Button
			variant={buttonVariant}
			onClick={handleClick}
			disabled={disabled || isWaiting}
			sx={{
				position: "relative",
				display: "flex",
				alignItems: "center",
				justifyContent: "center",
				maxHeight: { maxHeight },
			}}
		>
			{isWaiting && (
				<CircularProgress size={24} sx={{ position: "absolute" }} />
			)}
			{buttonText}
		</Button>
	);
};

export default ActionButton;
