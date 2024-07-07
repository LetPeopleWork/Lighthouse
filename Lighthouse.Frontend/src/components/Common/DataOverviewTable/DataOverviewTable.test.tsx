import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import { ILighthouseData } from '../../../models/ILighthouseData';
import DataOverviewTable from './DataOverviewTable';

const renderWithRouter = (ui : React.ReactNode) => {
    return render(<BrowserRouter>{ui}</BrowserRouter>);
};

const sampleData: ILighthouseData[] = [
    { id: 1, name: 'Item 1', remainingWork: 10, features: 5 },
    { id: 2, name: 'Item 2', remainingWork: 20, features: 15 },
    { id: 3, name: 'Another Item', remainingWork: 30, features: 25 },
];

describe('DataOverviewTable', () => {
    it('renders correctly', () => {
        renderWithRouter(<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />);
        expect(screen.getByTestId('table-container')).toBeInTheDocument();
    });

    it('displays all items from the data passed in', () => {
        renderWithRouter(<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />);
        sampleData.forEach(item => {
            expect(screen.getByTestId(`table-row-${item.id}`)).toBeInTheDocument();
            expect(screen.getByText(item.name)).toBeInTheDocument();
        });
    });

    it('filters properly', () => {
        renderWithRouter(<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />);
        const filterInput = screen.getByPlaceholderText('Search');
        
        fireEvent.change(filterInput, { target: { value: 'Item' } });
        expect(screen.getByText('Item 1')).toBeInTheDocument();
        expect(screen.getByText('Item 2')).toBeInTheDocument();
        expect(screen.queryByText('Another Item')).toBeInTheDocument();
        
        fireEvent.change(filterInput, { target: { value: 'Another' } });
        expect(screen.queryByText('Item 1')).not.toBeInTheDocument();
        expect(screen.queryByText('Item 2')).not.toBeInTheDocument();
        expect(screen.getByText('Another Item')).toBeInTheDocument();
    });

    it('displays the custom message when no item matches filter', () => {
        renderWithRouter(<DataOverviewTable data={sampleData} api="api" onDelete={vi.fn()} />);
        const filterInput = screen.getByPlaceholderText('Search');
        
        fireEvent.change(filterInput, { target: { value: 'Non-existing Item' } });
        expect(screen.getByTestId('no-items-message')).toBeInTheDocument();
    });
});
