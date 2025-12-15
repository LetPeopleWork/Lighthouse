import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Typography } from "@mui/material";
import type React from "react";

interface DeliveryHeaderProps {
	onAddDelivery: () => void;
}

export const DeliveryHeader: React.FC<DeliveryHeaderProps> = ({
	onAddDelivery,
}) => {
	return (
		<Box
			display="flex"
			justifyContent="space-between"
			alignItems="center"
			mb={2}
		>
			<Typography variant="h5">Deliveries</Typography>
			<Button
				variant="outlined"
				startIcon={<AddIcon />}
				onClick={onAddDelivery}
			>
				Add Delivery
			</Button>
		</Box>
	);
};

export default DeliveryHeader;
