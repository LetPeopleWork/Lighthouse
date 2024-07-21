import React, { useState } from 'react';
import { Card, CardHeader, CardContent, Grid, IconButton, Collapse } from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';

interface InputGroupProps {
    title: string;
    children: React.ReactNode;
    initiallyExpanded?: boolean;
}

const InputGroup: React.FC<InputGroupProps> = ({ title, children, initiallyExpanded = true }) => {
    const [expanded, setExpanded] = useState(initiallyExpanded);

    const handleToggle = () => {
        setExpanded(prev => !prev);
    };

    return (
        <Card variant="outlined" style={{ width: '100%', marginTop: 5 }}>
            <CardHeader
                title={title}
                action={
                    <IconButton
                        aria-label="toggle"
                        onClick={handleToggle}
                    >
                        {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                    </IconButton>
                }
            />
            <Collapse in={expanded}>
                <CardContent>
                    <Grid container spacing={2} direction="column">
                        {React.Children.map(children, (child) => (
                            <Grid item xs={12} style={{ width: '100%' }}>
                                {child}
                            </Grid>
                        ))}
                    </Grid>
                </CardContent>
            </Collapse>
        </Card>
    );
};

export default InputGroup;