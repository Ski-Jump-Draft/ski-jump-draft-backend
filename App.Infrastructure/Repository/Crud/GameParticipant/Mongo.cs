using App.Domain.Game;
using App.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using MongoDB.Driver;

namespace App.Infrastructure.Repository.Crud.GameParticipant
{
    public class Mongo : IGameParticipantRepository
    {
        private readonly IMongoCollection<Participant.Participant> _col;

        public Mongo(
            IMongoClient   client,
            IConfiguration cfg)
        {
            var dbName  = cfg["Mongo:DatabaseName"] 
                          ?? throw new ArgumentNullException("Mongo:DatabaseName");
            var colName = cfg["Mongo:GameParticipantCollection"] ?? "gameParticipants";
            var db      = client.GetDatabase(dbName);
            _col        = db.GetCollection<Participant.Participant>(colName);
        }

        public FSharpAsync<FSharpOption<Participant.Participant>> GetByIdAsync(Participant.Id id)
        {
            var filter   = Builders<Participant.Participant>
                             .Filter.Eq(p => p.Id.Item, id.Item);
            var findTask = _col.Find(filter).FirstOrDefaultAsync();

            // Project Task<Participant> -> Task<FSharpOption<Participant>>
            var optTask = findTask.ContinueWith(t =>
                t.Result is { }
                    ? FSharpOption<Participant.Participant>.Some(t.Result)
                    : FSharpOption<Participant.Participant>.None
            );

            return FSharpAsync.AwaitTask(optTask);
        }

        public FSharpAsync<Unit> SaveAsync(Participant.Participant participant)
        {
            var filter  = Builders<Participant.Participant>
                            .Filter.Eq(p => p.Id.Item, participant.Id.Item);
            var options = new ReplaceOptions { IsUpsert = true };
            var upsert  = _col.ReplaceOneAsync(filter, participant, options);

            // Project Task<ReplaceOneResult> -> Task<Unit>
            var unitTask = upsert.ContinueWith(_ => default(Unit));
            return FSharpAsync.Ignore(FSharpAsync.AwaitTask(unitTask));
        }

        public FSharpAsync<Unit> RemoveAsync(Participant.Id id)
        {
            var filter   = Builders<Participant.Participant>
                             .Filter.Eq(p => p.Id.Item, id.Item);
            var deleteOp = _col.DeleteOneAsync(filter);

            // Project Task<DeleteResult> -> Task<Unit>
            var unitTask = deleteOp.ContinueWith(_ => default(Unit));
            return FSharpAsync.Ignore(FSharpAsync.AwaitTask(unitTask));
        }
    }
}
