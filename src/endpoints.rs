use actix_web::{
    post,
    web::{Data, Json, Path},
    App, HttpResponse, HttpServer, Result,
};
use diesel::prelude::*;
use diesel_async::{
    pg::AsyncPgConnection,
    pooled_connection::{bb8::Pool, AsyncDieselConnectionManager},
    RunQueryDsl,
};

use crate::models::{AppState, Transaction, TransactionRequest, TransactionResponse};

#[post("/clientes/{id}/transacoes")]
pub async fn post_transaction(
    target_account_id: Path<i32>,
    transaction: Json<TransactionRequest>,
    data: Data<AppState>,
) -> Result<HttpResponse> {
    use crate::schema::transactions::dsl::*;
    let transaction = transaction.into_inner();

    if !transaction.is_valid() {
        return Ok(HttpResponse::UnprocessableEntity().finish());
    }

    let pool = data.as_ref().pool.clone();

    let conn = &mut pool.get().await.expect("error");

    let transaction: Vec<Transaction> = match transactions
        .filter(
            crate::schema::transactions::dsl::accountid
                .eq(target_account_id.abs()),
        )
        .select(Transaction::as_select())
        .load(conn)
        .await
    {
        Ok(account) => account,
        Err(error) => {
            println!("Error when querying accounts: {}", error);
            vec![]
        }
    };

    Ok(HttpResponse::Ok().json(TransactionResponse {
        limite: 0,
        saldo: 0,
    }))
}