import React from 'react';
import { Card, CardHeader, CardContent, Grid } from '@mui/material';

interface InputGroupProps {
    title: string;
    children: React.ReactNode;
}

const InputGroup: React.FC<InputGroupProps> = ({ title, children }) => {
    return (
        <Card variant="outlined" style={{ width: '100%', marginTop: 5 }}>
            <CardHeader title={title} />
            <CardContent>
                <Grid container spacing={2} direction="column">
                    {React.Children.map(children, (child) => (
                        <Grid item xs={12} style={{ width: '100%' }}>
                            {child}
                        </Grid>
                    ))}
                </Grid>
            </CardContent>
        </Card>
    );
};

export default InputGroup;
