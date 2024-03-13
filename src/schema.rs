use diesel::allow_tables_to_appear_in_same_query;

diesel::table! {
    transactions (id) {
        id -> Int4,
        tipo -> Text,
        descricao -> Text,
        saldo -> Int4,
        limite -> Int4,
        accountid -> Int4,
        valor -> Int4,
        realizadaem -> Timestamp 
    }
}

diesel::table! {
    accounts (id) {
        id -> Int4,
        saldo -> Int4,
        limite -> Int4,
    }
}

diesel::joinable!(transactions -> accounts(accountid));

allow_tables_to_appear_in_same_query!(transactions, accounts);