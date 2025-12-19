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
		<Box>
			<Box
				display="flex"
				justifyContent="space-between"
				alignItems="flex-start"
				mb={2}
			>
				<Box>
					<Typography variant="h5" sx={{ mb: 1 }}>
						{deliveriesTerm}
					</Typography>
				</Box>
				<Button
					variant="outlined"
					startIcon={<AddIcon />}
					onClick={onAddDelivery}
				>
					Add {deliveryTerm}
				</Button>
			</Box>
		</Box>
	);
};

export default DeliveryHeader;
