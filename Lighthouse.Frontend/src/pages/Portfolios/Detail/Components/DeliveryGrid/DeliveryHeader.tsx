import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Typography } from "@mui/material";
import type React from "react";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface DeliveryHeaderProps {
	onAddDelivery: () => void;
}

export const DeliveryHeader: React.FC<DeliveryHeaderProps> = ({
	onAddDelivery,
}) => {
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	return (
		<Box
			display="flex"
			justifyContent="space-between"
			alignItems="center"
			mb={2}
		>
			<Typography variant="h5">{deliveriesTerm}</Typography>
			<Button
				variant="outlined"
				startIcon={<AddIcon />}
				onClick={onAddDelivery}
			>
				Add {deliveryTerm}
			</Button>
		</Box>
	);
};

export default DeliveryHeader;
