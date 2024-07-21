import { InputAdornment, TextField } from "@mui/material";
import SearchIcon from '@mui/icons-material/Search';

interface FilterBarProps {
  filterText: string;
  onFilterTextChange: (newFilterText: string) => void;
}

const FilterBar: React.FC<FilterBarProps> = ({ filterText, onFilterTextChange }) => {
  return (
    <TextField
      margin="normal"
      id="standard-basic"
      placeholder="Search"
      variant="standard"
      value={filterText}
      onChange={(e) => onFilterTextChange(e.target.value)}
      fullWidth
      InputProps={{
        startAdornment: (
          <InputAdornment position="start">
            <SearchIcon />
          </InputAdornment>
        ),
      }}
    />
  );
}

export default FilterBar;