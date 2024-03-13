use chrono::NaiveDateTime;
use diesel::{Identifiable, Queryable, Selectable};
use diesel_async::AsyncPgConnection;
use diesel_async::pooled_connection::bb8::Pool;
use serde::{Deserialize, Serialize};

#[derive(Clone)]
pub struct AppState {
    pub pool: Pool<AsyncPgConnection>,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct TransactionRequest {
    pub tipo: char,
    pub valor: u32,
    pub descricao: String,
}

impl TransactionRequest {
    pub fn is_valid(&self) -> bool {
        (self.tipo == 'c' || self.tipo == 'd')
        && self.valor > 0
        && self.descricao.is_empty() 
        && self.descricao.len() <= 10
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct TransactionResponse {
    pub limite: i32,
    pub saldo: i32,
}

#[derive(Queryable, Identifiable, Selectable, Debug, PartialEq)]
#[diesel(table_name = crate::schema::transactions)]
#[diesel(check_for_backend(diesel::pg::Pg))]
pub struct Transaction {
    id: i32,
    tipo: String,
    valor: i32,
    descricao: String,
    realizadaem: NaiveDateTime,
    accountid: i32,
}

#[derive(Queryable, Identifiable, Selectable, Debug, PartialEq)]
#[diesel(table_name = crate::schema::accounts)]
#[diesel(check_for_backend(diesel::pg::Pg))]
pub struct Account {
    id: i32,
    saldo: i32,
    limite: i32,
}
