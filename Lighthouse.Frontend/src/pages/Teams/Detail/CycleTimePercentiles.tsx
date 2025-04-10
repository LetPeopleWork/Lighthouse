import {
	Card,
	CardContent,
	Table,
	TableBody,
	TableCell,
	TableRow,
	Typography,
} from "@mui/material";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";

interface CycleTimePercentilesProps {
	percentileValues: IPercentileValue[];
}

const CycleTimePercentiles: React.FC<CycleTimePercentilesProps> = ({
	percentileValues,
}) => {
	const formatDays = (days: number): string => {
		return days === 1 ? `${days.toFixed(0)} day` : `${days.toFixed(0)} days`;
	};

	// Get forecast level based on percentile
	const getForecastLevel = (percentile: number) => {
		return new ForecastLevel(percentile);
	};

	return (
		<Card
			sx={{ m:2, p:1, borderRadius: 2, cursor: "pointer" }}
		>
			<CardContent>
				<Typography variant="h6" gutterBottom>
					Cycle Time Percentiles
				</Typography>
				{percentileValues.length > 0 ? (
					<Table size="small">
						<TableBody>
              {percentileValues
                .slice()
                .sort((a, b) => b.percentile - a.percentile)
                .map((item) => {
                  const forecastLevel = getForecastLevel(item.percentile);
                  const IconComponent = forecastLevel.IconComponent;

                  return (
                    <TableRow key={item.percentile}>
                      <TableCell sx={{ border: 0, padding: "4px 0" }}>
                        <Typography
                          variant="body2"
                          sx={{ display: "flex", alignItems: "center" }}
                        >
                          <IconComponent
                            fontSize="small"
                            sx={{ color: forecastLevel.color, mr: 1 }}
                          />
                          {item.percentile}th
                        </Typography>
                      </TableCell>
                      <TableCell
                        align="right"
                        sx={{ border: 0, padding: "4px 0" }}
                      >
                        <Typography
                          variant="body1"
                          fontWeight="bold"
                          sx={{ color: forecastLevel.color }}
                        >
                          {formatDays(item.value)}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  );
                })}
						</TableBody>
					</Table>
				) : (
					<Typography variant="body2" color="text.secondary">
						No data available
					</Typography>
				)}
			</CardContent>
		</Card>
	);
};

export default CycleTimePercentiles;
