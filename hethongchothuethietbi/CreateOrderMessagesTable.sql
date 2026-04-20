-- Create OrderMessages table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderMessages')
BEGIN
    CREATE TABLE [OrderMessages] (
        [Id] int NOT NULL IDENTITY(1,1),
        [OrderId] int NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderMessages_RentalOrders_OrderId] FOREIGN KEY ([OrderId]) 
            REFERENCES [RentalOrders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderMessages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) 
            REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );

    -- Create indexes
    CREATE INDEX [IX_OrderMessages_OrderId] ON [OrderMessages] ([OrderId]);
    CREATE INDEX [IX_OrderMessages_SenderId] ON [OrderMessages] ([SenderId]);

    PRINT 'OrderMessages table created successfully.';
END
ELSE
BEGIN
    PRINT 'OrderMessages table already exists.';
END
