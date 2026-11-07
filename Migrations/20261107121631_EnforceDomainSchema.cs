using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk_Flow.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDomainSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_AssignedToId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_RequesterId",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketId",
                table: "Comments");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "tickets");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "comments");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "users",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "tickets",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "tickets",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "tickets",
                newName: "priority");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "tickets",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tickets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "tickets",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "ResolvedAt",
                table: "tickets",
                newName: "resolved_at");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "tickets",
                newName: "requester_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tickets",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                table: "tickets",
                newName: "closed_at");

            migrationBuilder.RenameColumn(
                name: "AssignedToId",
                table: "tickets",
                newName: "assigned_to_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_RequesterId",
                table: "tickets",
                newName: "IX_tickets_requester_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_AssignedToId",
                table: "tickets",
                newName: "IX_tickets_assigned_to_id");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "comments",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "comments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "comments",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "comments",
                newName: "ticket_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "comments",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AuthorId",
                table: "comments",
                newName: "author_id");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_AuthorId",
                table: "comments",
                newName: "IX_comments_author_id");

            migrationBuilder.Sql("""ALTER TABLE "users" ALTER COLUMN "role" TYPE varchar(30) USING (CASE "role" WHEN 0 THEN 'User' WHEN 1 THEN 'Technician' WHEN 2 THEN 'Admin' END)""");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "tickets",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql("""ALTER TABLE "tickets" ALTER COLUMN "status" TYPE varchar(30) USING (CASE "status" WHEN 0 THEN 'Open' WHEN 1 THEN 'InProgress' WHEN 2 THEN 'Resolved' WHEN 3 THEN 'Closed' WHEN 4 THEN 'Cancelled' END)""");

            migrationBuilder.Sql("""ALTER TABLE "tickets" ALTER COLUMN "priority" TYPE varchar(20) USING (CASE "priority" WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' WHEN 2 THEN 'High' WHEN 3 THEN 'Critical' END)""");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "tickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tickets",
                table: "tickets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comments",
                table: "comments",
                column: "id");

            migrationBuilder.Sql("""UPDATE users SET email = lower(btrim(email))""");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_created_at",
                table: "tickets",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_priority",
                table: "tickets",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_status",
                table: "tickets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_status_priority",
                table: "tickets",
                columns: new[] { "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_comments_ticket_id_created_at",
                table: "comments",
                columns: new[] { "ticket_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_comments_tickets_ticket_id",
                table: "comments",
                column: "ticket_id",
                principalTable: "tickets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_users_author_id",
                table: "comments",
                column: "author_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_assigned_to_id",
                table: "tickets",
                column: "assigned_to_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_requester_id",
                table: "tickets",
                column: "requester_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_tickets_ticket_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_users_author_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_assigned_to_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_requester_id",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tickets",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_created_at",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_priority",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_status",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_status_priority",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_comments",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_ticket_id_created_at",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "tickets");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "tickets",
                newName: "Tickets");

            migrationBuilder.RenameTable(
                name: "comments",
                newName: "Comments");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Tickets",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Tickets",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "priority",
                table: "Tickets",
                newName: "Priority");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Tickets",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tickets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Tickets",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "resolved_at",
                table: "Tickets",
                newName: "ResolvedAt");

            migrationBuilder.RenameColumn(
                name: "requester_id",
                table: "Tickets",
                newName: "RequesterId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Tickets",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "closed_at",
                table: "Tickets",
                newName: "ClosedAt");

            migrationBuilder.RenameColumn(
                name: "assigned_to_id",
                table: "Tickets",
                newName: "AssignedToId");

            migrationBuilder.RenameIndex(
                name: "IX_tickets_requester_id",
                table: "Tickets",
                newName: "IX_Tickets_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_tickets_assigned_to_id",
                table: "Tickets",
                newName: "IX_Tickets_AssignedToId");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Comments",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Comments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Comments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ticket_id",
                table: "Comments",
                newName: "TicketId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Comments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "author_id",
                table: "Comments",
                newName: "AuthorId");

            migrationBuilder.RenameIndex(
                name: "IX_comments_author_id",
                table: "Comments",
                newName: "IX_Comments_AuthorId");

            migrationBuilder.Sql("""ALTER TABLE "Users" ALTER COLUMN "Role" TYPE integer USING (CASE "Role" WHEN 'User' THEN 0 WHEN 'Technician' THEN 1 WHEN 'Admin' THEN 2 END)""");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(180)",
                oldMaxLength: 180);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.Sql("""ALTER TABLE "Tickets" ALTER COLUMN "Status" TYPE integer USING (CASE "Status" WHEN 'Open' THEN 0 WHEN 'InProgress' THEN 1 WHEN 'Resolved' THEN 2 WHEN 'Closed' THEN 3 WHEN 'Cancelled' THEN 4 END)""");

            migrationBuilder.Sql("""ALTER TABLE "Tickets" ALTER COLUMN "Priority" TYPE integer USING (CASE "Priority" WHEN 'Low' THEN 0 WHEN 'Medium' THEN 1 WHEN 'High' THEN 2 WHEN 'Critical' THEN 3 END)""");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketId",
                table: "Comments",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_AssignedToId",
                table: "Tickets",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_RequesterId",
                table: "Tickets",
                column: "RequesterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
