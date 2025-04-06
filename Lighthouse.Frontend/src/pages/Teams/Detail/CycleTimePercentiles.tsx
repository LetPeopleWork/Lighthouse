import {
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableRow,
  Dialog,
  DialogTitle,
  DialogContent,
  IconButton,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import { useState } from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";

interface CycleTimePercentilesProps {
  percentileValues: IPercentileValue[];
}

const CycleTimePercentiles: React.FC<CycleTimePercentilesProps> = ({
  percentileValues,
}) => {
  const [open, setOpen] = useState(false);

  const handleOpen = () => setOpen(true);
  const handleClose = () => setOpen(false);

  // Format days to a more readable format
  const formatDays = (days: number): string => {
    if (days < 1) {
      return `${Math.round(days * 24)} hours`;
    }
    return days === 1 ? `${days} day` : `${days.toFixed(1)} days`;
  };

  return (
    <>
      <Card
        sx={{ p: 2, borderRadius: 2, cursor: "pointer" }}
        onClick={handleOpen}
      >
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Cycle Time Percentiles
          </Typography>
          {percentileValues.length > 0 ? (
            <Table size="small">
              <TableBody>
                {percentileValues.map((item) => (
                  <TableRow key={item.percentile}>
                    <TableCell sx={{ border: 0, padding: "4px 0" }}>
                      <Typography variant="body2">{item.percentile}th</Typography>
                    </TableCell>
                    <TableCell align="right" sx={{ border: 0, padding: "4px 0" }}>
                      <Typography variant="body1" fontWeight="bold">
                        {formatDays(item.value)}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No data available
            </Typography>
          )}
        </CardContent>
      </Card>

      <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
        <DialogTitle>
          Cycle Time Percentiles
          <IconButton
            onClick={handleClose}
            sx={{ position: "absolute", right: 8, top: 8 }}
          >
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          {percentileValues.length > 0 ? (
            <Table>
              <TableBody>
                {percentileValues
                  .sort((a, b) => a.percentile - b.percentile)
                  .map((item) => (
                    <TableRow key={item.percentile}>
                      <TableCell>
                        {item.percentile}th Percentile
                      </TableCell>
                      <TableCell align="right">
                        {formatDays(item.value)}
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No percentile data available
            </Typography>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
};

export default CycleTimePercentiles;
