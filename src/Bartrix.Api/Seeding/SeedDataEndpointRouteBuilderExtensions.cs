using Npgsql;

namespace Bartrix.Api.Seeding;

public static class SeedDataEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/seed").WithTags("Seed");

        group.MapPost("/reset", async (NpgsqlDataSource ds, CancellationToken ct) =>
        {
            await using var conn = await ds.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                TRUNCATE TABLE
                    reputation.reputation_reviews,
                    delivery.trade_deliveries,
                    messaging.conversation_messages,
                    messaging.conversations,
                    trades.trade_proposal_offered_listings,
                    trades.trade_proposals,
                    listings.listing_favorites,
                    listings.listings,
                    notifications.notifications,
                    payments.payments,
                    wallet.transactions,
                    services.service_offers,
                    reports.reports,
                    categories.category_suggestions,
                    categories.approved_categories,
                    withdrawals.withdrawal_requests,
                    auth.refresh_token_sessions,
                    auth.user_accounts
                RESTART IDENTITY CASCADE;
            ", conn);
            await cmd.ExecuteNonQueryAsync(ct);
            return Results.Ok(new { reset = true });
        });

        group.MapPost("/full", async (NpgsqlDataSource ds, CancellationToken ct) =>
        {
            await using var conn = await ds.OpenConnectionAsync(ct);

            // Users (password = Test1234! hashed with PBKDF2-SHA256, 100k iterations)
            const string hash = "pbkdf2-v1.100000.o5ni0H0K2hidlpq59GfZlw==.8hY+mLJmBhdqhmtVLD9Tx4yVR5DWb3CbT9EpH5OJoEE=";

            var users = new[]
            {
                (Guid.Parse("11111111-1111-1111-1111-111111111111"), "admin@bartix.test",    "Admin User",    "admin",     false, false),
                (Guid.Parse("22222222-2222-2222-2222-222222222222"), "agent@bartix.test",    "Support Agent", "agent",     false, false),
                (Guid.Parse("33333333-3333-3333-3333-333333333333"), "premium1@bartix.test", "Alice Premium", "user",      true,  false),
                (Guid.Parse("44444444-4444-4444-4444-444444444444"), "premium2@bartix.test", "Bob Premium",   "user",      true,  false),
                (Guid.Parse("55555555-5555-5555-5555-555555555555"), "user1@bartix.test",    "Charlie",       "user",      false, false),
                (Guid.Parse("66666666-6666-6666-6666-666666666666"), "user2@bartix.test",    "Diana",         "user",      false, false),
                (Guid.Parse("77777777-7777-7777-7777-777777777777"), "user3@bartix.test",    "Eve",           "user",      false, false),
                (Guid.Parse("88888888-8888-8888-8888-888888888888"), "user4@bartix.test",    "Frank",         "user",      false, true ),
                (Guid.Parse("99999999-9999-9999-9999-999999999999"), "user5@bartix.test",    "Grace",         "user",      false, false),
                (Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "user6@bartix.test",    "Hank",          "user",      false, false),
            };

            foreach (var (id, email, name, role, isPremium, isSuspended) in users)
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO auth.user_accounts
                        (id, email, normalized_email, display_name, password_hash,
                         is_phone_verified, created_at_utc,
                         role, is_suspended, is_premium_active,
                         premium_expires_at_utc, wallet_balance, language_code)
                    VALUES ($1,$2,UPPER($2),$3,$4,
                        false, NOW(),
                        $5,$6,$7,
                        CASE WHEN $7 THEN NOW() + INTERVAL '30 days' ELSE NULL END,
                        100.00, 'en')
                    ON CONFLICT (id) DO NOTHING;", conn);
                cmd.Parameters.Add(new NpgsqlParameter { Value = id });
                cmd.Parameters.Add(new NpgsqlParameter { Value = email });
                cmd.Parameters.Add(new NpgsqlParameter { Value = name });
                cmd.Parameters.Add(new NpgsqlParameter { Value = hash });
                cmd.Parameters.Add(new NpgsqlParameter { Value = role });
                cmd.Parameters.Add(new NpgsqlParameter { Value = isSuspended });
                cmd.Parameters.Add(new NpgsqlParameter { Value = isPremium });
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Categories
            var cats = new[] { "Electronics", "Clothing", "Books", "Sports", "Furniture", "Services" };
            foreach (var cat in cats)
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO categories.approved_categories (id, name, added_by, added_by_name, added_at_utc)
                    VALUES (gen_random_uuid(), $1, '11111111-1111-1111-1111-111111111111', 'Admin User', NOW())
                    ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.Add(new NpgsqlParameter { Value = cat });
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Listings (20 items, spread across users)
            var listingData = new[]
            {
                ("iPhone 13 Pro",          "55555555-5555-5555-5555-555555555555", "Electronics", "new",            500m,  true,  true),
                ("Samsung Galaxy S22",     "66666666-6666-6666-6666-666666666666", "Electronics", "like_new",       350m,  true,  false),
                ("MacBook Air M1",         "77777777-7777-7777-7777-777777777777", "Electronics", "good",           800m,  false, true),
                ("Sony Headphones",        "88888888-8888-8888-8888-888888888888", "Electronics", "good",           150m,  true,  true),
                ("Vintage Denim Jacket",   "99999999-9999-9999-9999-999999999999", "Clothing",    "good",           80m,   true,  false),
                ("Nike Air Max 270",       "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "Clothing",    "like_new",       120m,  true,  true),
                ("Harry Potter Set",       "55555555-5555-5555-5555-555555555555", "Books",       "good",           40m,   true,  true),
                ("Python Programming",    "66666666-6666-6666-6666-666666666666", "Books",       "new",            60m,   false, true),
                ("Mountain Bike",         "77777777-7777-7777-7777-777777777777", "Sports",      "good",           400m,  true,  true),
                ("Yoga Mat",              "88888888-8888-8888-8888-888888888888", "Sports",      "new",            35m,   true,  false),
                ("Coffee Table",          "99999999-9999-9999-9999-999999999999", "Furniture",   "good",           200m,  true,  false),
                ("Office Chair",          "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "Furniture",   "like_new",       300m,  false, true),
                ("PlayStation 5",         "33333333-3333-3333-3333-333333333333", "Electronics", "like_new",       600m,  true,  true),
                ("iPad Pro 2022",         "44444444-4444-4444-4444-444444444444", "Electronics", "new",            900m,  true,  true),
                ("Leather Sofa",          "55555555-5555-5555-5555-555555555555", "Furniture",   "good",           700m,  true,  false),
                ("Treadmill",             "66666666-6666-6666-6666-666666666666", "Sports",      "good",           450m,  true,  true),
                ("DSLR Camera",           "77777777-7777-7777-7777-777777777777", "Electronics", "like_new",       750m,  true,  false),
                ("Dining Table",          "88888888-8888-8888-8888-888888888888", "Furniture",   "new",            550m,  false, true),
                ("Electric Guitar",       "99999999-9999-9999-9999-999999999999", "Electronics", "good",           380m,  true,  true),
                ("Winter Coat",           "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "Clothing",    "new",            190m,  true,  false),
            };

            var listingIds = new List<Guid>();
            foreach (var (title, ownerId, cat, cond, price, forSwap, forSale) in listingData)
            {
                var lid = Guid.NewGuid();
                listingIds.Add(lid);
                var isP = ownerId is "33333333-3333-3333-3333-333333333333" or "44444444-4444-4444-4444-444444444444";
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO listings.listings
                        (id, owner_user_id, title, description, category, location, asking_price,
                         condition, is_available_for_swap, is_available_for_sale,
                         is_owner_premium, is_active, status, view_count, created_at_utc, updated_at_utc)
                    VALUES ($1,$2,$3,$4,$5,'Cairo, Egypt',$6,$7,$8,$9,$10,true,'active',0,NOW(),NOW())
                    ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.Add(new NpgsqlParameter { Value = lid });
                cmd.Parameters.Add(new NpgsqlParameter { Value = Guid.Parse(ownerId) });
                cmd.Parameters.Add(new NpgsqlParameter { Value = title });
                cmd.Parameters.Add(new NpgsqlParameter { Value = $"A {cond} {title} available for trade." });
                cmd.Parameters.Add(new NpgsqlParameter { Value = cat });
                cmd.Parameters.Add(new NpgsqlParameter { Value = price });
                cmd.Parameters.Add(new NpgsqlParameter { Value = cond });
                cmd.Parameters.Add(new NpgsqlParameter { Value = forSwap });
                cmd.Parameters.Add(new NpgsqlParameter { Value = forSale });
                cmd.Parameters.Add(new NpgsqlParameter { Value = isP });
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Trades (8 trade proposals)
            var tradeStatuses = new[] { "Pending","Pending","Accepted","Accepted","Completed","Completed","Rejected","Expired" };
            for (int i = 0; i < 8 && i + 1 < listingIds.Count; i++)
            {
                var senders   = new[] { "55555555-5555-5555-5555-555555555555",
                                        "66666666-6666-6666-6666-666666666666",
                                        "77777777-7777-7777-7777-777777777777",
                                        "88888888-8888-8888-8888-888888888888",
                                        "99999999-9999-9999-9999-999999999999",
                                        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                                        "33333333-3333-3333-3333-333333333333",
                                        "44444444-4444-4444-4444-444444444444" };
                var receivers = new[] { "66666666-6666-6666-6666-666666666666",
                                        "77777777-7777-7777-7777-777777777777",
                                        "88888888-8888-8888-8888-888888888888",
                                        "99999999-9999-9999-9999-999999999999",
                                        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                                        "55555555-5555-5555-5555-555555555555",
                                        "55555555-5555-5555-5555-555555555555",
                                        "66666666-6666-6666-6666-666666666666" };

                var tradeId = Guid.NewGuid();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO trades.trade_proposals
                        (id, sender_user_id, receiver_user_id, requested_listing_id,
                         message, status, type, created_at_utc, updated_at_utc, expires_at_utc)
                    VALUES ($1,$2,$3,$4,'Looking to trade!',$5,'any',NOW(),NOW(),$6)
                    ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.Add(new NpgsqlParameter { Value = tradeId });
                cmd.Parameters.Add(new NpgsqlParameter { Value = Guid.Parse(senders[i]) });
                cmd.Parameters.Add(new NpgsqlParameter { Value = Guid.Parse(receivers[i]) });
                cmd.Parameters.Add(new NpgsqlParameter { Value = listingIds[i + 1] });
                cmd.Parameters.Add(new NpgsqlParameter { Value = tradeStatuses[i] });
                cmd.Parameters.Add(new NpgsqlParameter { Value = DateTimeOffset.UtcNow.AddDays(5) });
                await cmd.ExecuteNonQueryAsync(ct);

                // offered listing in junction table
                await using var offerCmd = new NpgsqlCommand(@"
                    INSERT INTO trades.trade_proposal_offered_listings (trade_proposal_id, listing_id)
                    VALUES ($1,$2) ON CONFLICT DO NOTHING;", conn);
                offerCmd.Parameters.Add(new NpgsqlParameter { Value = tradeId });
                offerCmd.Parameters.Add(new NpgsqlParameter { Value = listingIds[i] });
                await offerCmd.ExecuteNonQueryAsync(ct);
            }

            var summary = new
            {
                seeded = true,
                summary = new
                {
                    users    = users.Length,
                    listings = listingData.Length,
                    trades   = 8,
                    categories = cats.Length
                }
            };

            return Results.Ok(summary);
        });

        return app;
    }
}
