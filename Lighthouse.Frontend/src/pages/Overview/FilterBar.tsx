interface FilterBarProps {
  filterText: string;
  onFilterTextChange : (newFilterText: string) => void;
}

const FilterBar:React.FC<FilterBarProps> = ({ filterText, onFilterTextChange }) => {
    return (
        <form>
          <input type="text" placeholder="Search..." value={filterText} onChange={(e) => onFilterTextChange(e.target.value)}/>          
        </form>
      );
}

export default FilterBar;