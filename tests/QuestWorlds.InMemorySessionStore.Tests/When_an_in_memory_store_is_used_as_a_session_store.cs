using QuestWorlds.Session;
using QuestWorlds.SessionStore.ContractTests;

namespace QuestWorlds.InMemorySessionStore.Tests;

public class When_an_in_memory_store_is_used_as_a_session_store : SessionStoreContract
{
    protected override IAmASessionStore CreateStore() => new InMemorySessionStore();
}
