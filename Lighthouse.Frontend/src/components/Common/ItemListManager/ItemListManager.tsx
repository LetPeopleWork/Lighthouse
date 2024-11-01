import React, { useState } from 'react';
import { List, ListItem, Typography, IconButton, TextField, Button, Grid } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';

interface ItemListManagerProps {
    title: string;
    items: string[];
    onAddItem: (item: string) => void;
    onRemoveItem: (item: string) => void;
}

const ItemListManager: React.FC<ItemListManagerProps> = ({
    title,
    items,
    onAddItem,
    onRemoveItem,
}) => {
    const [newItem, setNewItem] = useState<string>('');

    const handleAddItem = () => {
        if (newItem.trim()) {
            onAddItem(newItem.trim());
            setNewItem('');
        }
    };

    const handleNewItemChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setNewItem(event.target.value);
    };

    return (
        <Grid container spacing={3}>
            <Grid item xs={3}>
                <List>
                    {items.filter(item => item.trim()).map(item => (
                        <ListItem key={item}>
                            <Typography variant="body1">{item}</Typography>
                            <IconButton aria-label="delete" edge="end" onClick={() => onRemoveItem(item)}>
                                <DeleteIcon />
                            </IconButton>
                        </ListItem>
                    ))}
                </List>
            </Grid>
            <Grid item xs={9}>
                <TextField
                    label={`New ${title}`}
                    fullWidth
                    margin="normal"
                    value={newItem}
                    onChange={handleNewItemChange}
                />
                <Button variant="outlined" color="primary" onClick={handleAddItem}>
                    Add {title}
                </Button>
            </Grid>
        </Grid>
    );
};

export default ItemListManager;